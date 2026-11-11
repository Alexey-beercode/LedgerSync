using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace LedgerSync.Infrastructure.Persistence.Repositories;

internal sealed class SyncLogRepository(LedgerSyncDbContext dbContext) : ISyncLogRepository
{
    public void Add(SyncLog syncLog) => dbContext.SyncLogs.Add(syncLog);

    public Task<SyncLog?> GetByIdAsync(Guid runId, CancellationToken cancellationToken) =>
        dbContext.SyncLogs.FirstOrDefaultAsync(s => s.Id == runId, cancellationToken);

    public Task<SyncLog?> GetLatestAsync(CancellationToken cancellationToken) =>
        dbContext.SyncLogs
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncLog>> GetRecentAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.SyncLogs
            .AsNoTracking()
            .Include(s => s.Errors)
            .OrderByDescending(s => s.StartedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncLog>> GetRunningAsync(CancellationToken cancellationToken) =>
        await dbContext.SyncLogs
            .Where(s => s.Status == SyncStatus.Running)
            .ToListAsync(cancellationToken);
}
