using LedgerSync.Domain.Entities;

namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Storage of synchronization run history.
/// </summary>
public interface ISyncLogRepository
{
    void Add(SyncLog syncLog);

    /// <summary>Returns the most recently started run, or null if there were none.</summary>
    Task<SyncLog?> GetLatestAsync(CancellationToken cancellationToken);

    /// <summary>Returns the latest runs ordered from newest to oldest.</summary>
    Task<IReadOnlyList<SyncLog>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
