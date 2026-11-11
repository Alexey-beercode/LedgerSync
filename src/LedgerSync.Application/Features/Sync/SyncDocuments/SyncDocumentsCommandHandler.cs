using System.Diagnostics;
using FluentValidation;
using LedgerSync.Application.Abstractions.Concurrency;
using LedgerSync.Application.Abstractions.ExternalSource;
using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.ExternalSource;
using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Logging;
using LedgerSync.Application.Mappings;
using LedgerSync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LedgerSync.Application.Features.Sync.SyncDocuments;

/// <summary>
/// Executes one synchronization run.
/// <para>Reliability rules:</para>
/// <list type="bullet">
/// <item>Only one run at a time (<see cref="ISyncRunLock"/>), parallel calls get <see cref="SyncAlreadyRunningException"/>.</item>
/// <item>The run is recorded as Running before any work, and always finalized as Success / PartialSuccess / Failed.</item>
/// <item>All document changes and run counters are saved in one transaction: all or nothing.</item>
/// <item>Google Sheets is contacted only after the commit and can never roll documents back.</item>
/// <item>Once started, a run is not bound to the HTTP request: a client disconnect cannot leave it half-done.</item>
/// </list>
/// </summary>
public sealed class SyncDocumentsCommandHandler(
    ISyncRunLock syncRunLock,
    IExternalDocumentsClient externalClient,
    IUnitOfWork unitOfWork,
    IValidator<ExternalDocumentPayload> payloadValidator,
    GoogleSheetReporter googleSheetReporter,
    TimeProvider timeProvider,
    ILogger<SyncDocumentsCommandHandler> logger) : ICommandHandler<SyncDocumentsCommand, SyncResultDto>
{
    private const string InterruptedRunMessage = "Run was interrupted before completion (process stopped or crashed).";

    public async Task<SyncResultDto> HandleAsync(SyncDocumentsCommand command, CancellationToken cancellationToken)
    {
        await using var runLock = await syncRunLock.TryAcquireAsync(cancellationToken);
        if (runLock is null)
        {
            logger.SyncAlreadyRunning();
            throw new SyncAlreadyRunningException();
        }

        // From here on the request token is no longer used: a started run is always completed and recorded.
        var token = CancellationToken.None;
        var stopwatch = Stopwatch.StartNew();

        await RecoverInterruptedRunsAsync(token);

        var syncLog = SyncLog.Start(timeProvider.GetUtcNow());
        unitOfWork.SyncLogs.Add(syncLog);
        await unitOfWork.SaveChangesAsync(token);

        // Every log entry of this run carries RunId, so one run can be traced end to end.
        using var scope = logger.BeginScope("RunId:{RunId}", syncLog.Id);
        logger.SyncStarted(syncLog.Id);

        syncLog = await ExecuteRunAsync(syncLog, token);

        logger.SyncFinished(syncLog.Id, syncLog.Status.Name, syncLog.DocumentsReceived, syncLog.DocumentsCreated,
            syncLog.DocumentsUpdated, syncLog.DocumentsSkipped, stopwatch.ElapsedMilliseconds);

        await googleSheetReporter.ReportAsync(syncLog, token);

        return syncLog.ToSyncResult();
    }

    private async Task<SyncLog> ExecuteRunAsync(SyncLog syncLog, CancellationToken token)
    {
        IReadOnlyList<ExternalDocumentPayload> payloads;
        try
        {
            payloads = await externalClient.FetchDocumentsAsync(token);
        }
        catch (ExternalSourceException ex)
        {
            logger.ExternalSourceFailed(ex, syncLog.Id);
            syncLog.Fail(ex.Message, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(token);
            return syncLog;
        }

        try
        {
            await ApplyDocumentsAsync(syncLog, payloads, token);

            // Single SaveChanges = single transaction: documents, counters and errors together.
            await unitOfWork.SaveChangesAsync(token);
            return syncLog;
        }
        catch (Exception ex)
        {
            logger.DatabaseSaveFailed(ex, syncLog.Id);
            return await FailAfterRollbackAsync(syncLog.Id, payloads.Count, ex, token);
        }
    }

    /// <summary>
    /// Validates the batch and applies create / update / skip decisions to tracked entities.
    /// Nothing is written to the database here.
    /// </summary>
    private async Task ApplyDocumentsAsync(
        SyncLog syncLog,
        IReadOnlyList<ExternalDocumentPayload> payloads,
        CancellationToken token)
    {
        syncLog.SetReceived(payloads.Count);

        var validDocuments = new List<ValidatedDocument>(payloads.Count);
        foreach (var payload in payloads)
        {
            var validation = await payloadValidator.ValidateAsync(payload, token);
            if (validation.IsValid)
            {
                validDocuments.Add(ValidatedDocument.From(payload));
                continue;
            }

            var reason = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            syncLog.RecordRejected(payload.ExternalId, reason);
            logger.DocumentRejected(payload.ExternalId, reason);
        }

        var batch = DeduplicateBatch(syncLog, validDocuments);

        // Two queries for the whole batch instead of two per document.
        var knownOrders = await unitOfWork.Orders.GetByNumbersAsync(
            batch.Select(d => d.OrderNumber).Distinct().ToList(), token);
        var existingDocuments = await unitOfWork.Documents.GetByExternalIdsAsync(
            batch.Select(d => d.ExternalId).ToList(), token);

        var now = timeProvider.GetUtcNow();
        foreach (var incoming in batch)
        {
            if (!knownOrders.TryGetValue(incoming.OrderNumber, out var order))
            {
                var reason = $"Order '{incoming.OrderNumber}' does not exist.";
                syncLog.RecordRejected(incoming.ExternalId, reason);
                logger.DocumentRejected(incoming.ExternalId, reason);
                continue;
            }

            if (!existingDocuments.TryGetValue(incoming.ExternalId, out var existing))
            {
                unitOfWork.Documents.Add(ExternalDocument.Create(
                    incoming.ExternalId, order, incoming.DocumentType, incoming.DocumentNumber,
                    incoming.Amount, incoming.Currency, incoming.Status, incoming.ExternalUpdatedAt, now));
                syncLog.RecordCreated();
            }
            else if (existing.IsNewerThan(incoming.ExternalUpdatedAt))
            {
                logger.StaleDocumentIgnored(incoming.ExternalId, incoming.ExternalUpdatedAt, existing.ExternalUpdatedAt);
                syncLog.RecordSkipped();
            }
            else if (existing.HasChanges(incoming.Status, incoming.Amount, incoming.ExternalUpdatedAt))
            {
                existing.ApplyExternalChanges(incoming.Status, incoming.Amount, incoming.ExternalUpdatedAt, now);
                syncLog.RecordUpdated();
            }
            else
            {
                syncLog.RecordSkipped();
            }
        }

        syncLog.Complete(timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Keeps only the latest version of a document that occurs several times in one response.
    /// Extra copies are counted as skipped, so counters still add up to "received".
    /// </summary>
    private List<ValidatedDocument> DeduplicateBatch(SyncLog syncLog, List<ValidatedDocument> validDocuments)
    {
        var result = new List<ValidatedDocument>(validDocuments.Count);
        foreach (var group in validDocuments.GroupBy(d => d.ExternalId, StringComparer.Ordinal))
        {
            var versions = group.OrderByDescending(d => d.ExternalUpdatedAt).ToList();
            result.Add(versions[0]);

            if (versions.Count == 1)
                continue;

            logger.DuplicateInBatch(group.Key, versions.Count);
            for (var i = 1; i < versions.Count; i++)
                syncLog.RecordSkipped();
        }

        return result;
    }

    /// <summary>
    /// After a failed save nothing from the run is in the database. Pending changes are dropped
    /// and the run is re-read and marked as Failed, so the error is still recorded.
    /// </summary>
    private async Task<SyncLog> FailAfterRollbackAsync(Guid runId, int received, Exception error, CancellationToken token)
    {
        unitOfWork.DiscardChanges();

        var syncLog = await unitOfWork.SyncLogs.GetByIdAsync(runId, token)
                      ?? throw new InvalidOperationException($"Sync run {runId} disappeared from the database.");

        syncLog.SetReceived(received);
        syncLog.Fail($"Document processing failed, no changes were applied: {error.Message}", timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(token);

        return syncLog;
    }

    /// <summary>
    /// Runs still marked as Running while we hold the lock were interrupted by a crash or restart.
    /// </summary>
    private async Task RecoverInterruptedRunsAsync(CancellationToken token)
    {
        var interrupted = await unitOfWork.SyncLogs.GetRunningAsync(token);
        if (interrupted.Count == 0)
            return;

        foreach (var run in interrupted)
        {
            run.Fail(InterruptedRunMessage, timeProvider.GetUtcNow());
            logger.InterruptedRunRecovered(run.Id);
        }

        await unitOfWork.SaveChangesAsync(token);
    }
}
