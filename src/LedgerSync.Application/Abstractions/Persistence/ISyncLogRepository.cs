using LedgerSync.Domain.Entities;

namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Storage of synchronization run history.
/// </summary>
public interface ISyncLogRepository
{
    void Add(SyncLog syncLog);

    Task<SyncLog?> GetByIdAsync(Guid runId, CancellationToken cancellationToken);

    /// <summary>Returns the most recently started run, or null if there were none.</summary>
    Task<SyncLog?> GetLatestAsync(CancellationToken cancellationToken);

    /// <summary>Returns the latest runs (with errors) ordered from newest to oldest.</summary>
    Task<IReadOnlyList<SyncLog>> GetRecentAsync(int take, CancellationToken cancellationToken);

    /// <summary>Returns runs still marked as Running (left over after a crash).</summary>
    Task<IReadOnlyList<SyncLog>> GetRunningAsync(CancellationToken cancellationToken);
}
