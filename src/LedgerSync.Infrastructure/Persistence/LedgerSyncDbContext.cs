using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the LedgerSync PostgreSQL database.
/// </summary>
public sealed class LedgerSyncDbContext(DbContextOptions<LedgerSyncDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<ExternalDocument> ExternalDocuments => Set<ExternalDocument>();

    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    public DbSet<SyncLogError> SyncLogErrors => Set<SyncLogError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerSyncDbContext).Assembly);
}
