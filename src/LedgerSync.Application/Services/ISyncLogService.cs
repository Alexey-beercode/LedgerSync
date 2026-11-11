using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Application.Services;

/// <summary>
/// Read access to sync history and manual re-delivery to Google Sheets.
/// </summary>
public interface ISyncLogService
{
    /// <exception cref="FluentValidation.ValidationException">Query parameters are invalid.</exception>
    Task<IReadOnlyList<SyncLogHistoryItemDto>> GetRecentAsync(SyncLogsQuery query, CancellationToken cancellationToken);

    /// <summary>Re-sends the latest sync run to Google Sheets (idempotent, upsert by RunId).</summary>
    /// <exception cref="SyncRunNotFoundException">There are no runs yet.</exception>
    /// <exception cref="SyncAlreadyRunningException">The latest run is still in progress.</exception>
    Task<GoogleSheetResendResultDto> ResendLatestToGoogleAsync(CancellationToken cancellationToken);
}
