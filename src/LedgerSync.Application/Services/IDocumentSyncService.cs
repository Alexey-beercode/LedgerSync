using LedgerSync.Application.Contracts.Sync;

namespace LedgerSync.Application.Services;

/// <summary>
/// Synchronizes documents from the external system into the internal database.
/// </summary>
public interface IDocumentSyncService
{
    /// <summary>
    /// Runs a full sync: fetch, create/update/skip, write SyncLog, report to Google Sheets.
    /// Never throws for source or Google errors — they are reflected in the result.
    /// </summary>
    Task<SyncResultDto> RunAsync(CancellationToken cancellationToken);
}
