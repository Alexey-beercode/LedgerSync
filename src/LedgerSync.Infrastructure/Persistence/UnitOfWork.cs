using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work. A single SaveChanges call is executed in one database transaction,
/// so either all pending changes are stored or none.
/// </summary>
internal sealed class UnitOfWork(LedgerSyncDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new PersistenceConflictException("Data was modified concurrently by another process.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new PersistenceConflictException($"Unique constraint '{pg.ConstraintName}' was violated.", ex);
        }
    }

    public void DiscardChanges() => dbContext.ChangeTracker.Clear();
}
