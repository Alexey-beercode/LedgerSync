using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Application.Services;

/// <summary>
/// Read access to sync history and manual re-delivery to Google Sheets.
/// </summary>
public interface ISyncLogService
{
    Task<IReadOnlyList<SyncLogHistoryItemDto>> GetRecentAsync(int take, CancellationToken cancellationToken);

    /// <summary>Re-sends the latest sync run to Google Sheets.</summary>
    /// <exception cref="SyncRunNotFoundException">There are no runs yet.</exception>
    Task<GoogleSheetResendResultDto> ResendLatestToGoogleAsync(CancellationToken cancellationToken);
}
