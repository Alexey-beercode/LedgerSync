namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Single entry point to persistence: repositories share one change tracker,
/// and <see cref="SaveChangesAsync"/> commits all their pending changes in one transaction.
/// </summary>
public interface IUnitOfWork
{
    IOrderRepository Orders { get; }

    IExternalDocumentRepository Documents { get; }

    ISyncLogRepository SyncLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Drops all pending (not yet saved) changes, e.g. after a failed save.
    /// Entities loaded before must be re-read afterwards.
    /// </summary>
    void DiscardChanges();
}
