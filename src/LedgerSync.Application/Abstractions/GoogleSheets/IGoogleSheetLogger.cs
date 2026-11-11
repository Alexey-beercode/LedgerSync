using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Application.Abstractions.GoogleSheets;

/// <summary>
/// Appends one row per sync run to the Google Sheets journal.
/// Implementations: mock (local file) or real integration (Apps Script / Sheets API).
/// </summary>
public interface IGoogleSheetLogger
{
    /// <exception cref="Common.Exceptions.GoogleSheetDeliveryException">Row could not be delivered.</exception>
    Task SendSyncLogAsync(SyncLogDto syncLog, CancellationToken cancellationToken);
}
