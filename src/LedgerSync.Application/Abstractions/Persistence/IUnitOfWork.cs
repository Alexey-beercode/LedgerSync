namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Commits all pending changes tracked by repositories as a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
