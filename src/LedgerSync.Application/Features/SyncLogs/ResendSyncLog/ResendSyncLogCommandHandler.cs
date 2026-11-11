using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Features.Sync;
using LedgerSync.Application.Mappings;

namespace LedgerSync.Application.Features.SyncLogs.ResendSyncLog;

/// <summary>
/// Delivers a finished run to Google Sheets again, e.g. after an outage.
/// </summary>
public sealed class ResendSyncLogCommandHandler(
    IUnitOfWork unitOfWork,
    GoogleSheetReporter googleSheetReporter) : ICommandHandler<ResendSyncLogCommand, GoogleSheetResendResultDto>
{
    /// <exception cref="SyncRunNotFoundException">The run (or any run, for "latest") does not exist.</exception>
    /// <exception cref="SyncAlreadyRunningException">The run is still in progress.</exception>
    public async Task<GoogleSheetResendResultDto> HandleAsync(ResendSyncLogCommand command, CancellationToken cancellationToken)
    {
        var run = command.RunId is { } runId
            ? await unitOfWork.SyncLogs.GetByIdAsync(runId, cancellationToken)
            : await unitOfWork.SyncLogs.GetLatestAsync(cancellationToken);

        if (run is null)
            throw new SyncRunNotFoundException(command.RunId);

        // A running run has no final numbers yet; sending it would publish a misleading row.
        if (!run.IsFinished)
            throw new SyncAlreadyRunningException();

        await googleSheetReporter.ReportAsync(run, CancellationToken.None);
        return run.ToResendResult();
    }
}
