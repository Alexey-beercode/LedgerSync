using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work. All repositories work on the same DbContext,
/// so a single SaveChanges call stores their changes in one database transaction: all or nothing.
/// </summary>
internal sealed class UnitOfWork(LedgerSyncDbContext dbContext) : IUnitOfWork
{
    private IOrderRepository? _orders;
    private IExternalDocumentRepository? _documents;
    private ISyncLogRepository? _syncLogs;

    public IOrderRepository Orders => _orders ??= new OrderRepository(dbContext);

    public IExternalDocumentRepository Documents => _documents ??= new ExternalDocumentRepository(dbContext);

    public ISyncLogRepository SyncLogs => _syncLogs ??= new SyncLogRepository(dbContext);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new PersistenceConflictException($"Unique constraint '{pg.ConstraintName}' was violated.", ex);
        }
    }

    public void DiscardChanges() => dbContext.ChangeTracker.Clear();
}
