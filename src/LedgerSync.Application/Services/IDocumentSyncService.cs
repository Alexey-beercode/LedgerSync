using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.Sync;

namespace LedgerSync.Application.Services;

/// <summary>
/// Synchronizes documents from the external system into the internal database.
/// </summary>
public interface IDocumentSyncService
{
    /// <summary>
    /// Runs a full sync: fetch, create/update/skip, write SyncLog, report to Google Sheets.
    /// Source, database and Google errors do not throw — they are reflected in the result and SyncLog.
    /// </summary>
    /// <exception cref="SyncAlreadyRunningException">Another run is in progress.</exception>
    Task<SyncResultDto> RunAsync(CancellationToken cancellationToken);
}
