namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Commits all pending changes tracked by repositories as a single atomic transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Drops all pending (not yet saved) changes, e.g. after a failed save.
    /// Entities loaded before must be re-read afterwards.
    /// </summary>
    void DiscardChanges();
}
