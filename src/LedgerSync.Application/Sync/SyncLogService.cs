using FluentValidation;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Mappings;
using LedgerSync.Application.Services;

namespace LedgerSync.Application.Sync;

/// <summary>
/// Sync history queries and manual resend of the latest run to Google Sheets.
/// </summary>
public sealed class SyncLogService(
    ISyncLogRepository syncLogs,
    IValidator<SyncLogsQuery> queryValidator,
    GoogleSheetReporter googleSheetReporter) : ISyncLogService
{
    public async Task<IReadOnlyList<SyncLogHistoryItemDto>> GetRecentAsync(SyncLogsQuery query, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var runs = await syncLogs.GetRecentAsync(query.Take, cancellationToken);
        return runs.Select(r => r.ToHistoryItem()).ToList();
    }

    public async Task<GoogleSheetResendResultDto> ResendLatestToGoogleAsync(CancellationToken cancellationToken)
    {
        var latest = await syncLogs.GetLatestAsync(cancellationToken) ?? throw new SyncRunNotFoundException();

        // A running run has no final numbers yet; sending it would publish a misleading row.
        if (!latest.IsFinished)
            throw new SyncAlreadyRunningException();

        await googleSheetReporter.ReportAsync(latest, CancellationToken.None);
        return latest.ToResendResult();
    }
}
