using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// Prepares the database on startup according to <see cref="PersistenceOptions"/>:
/// applies migrations and inserts demo orders. Both are off by default (production).
/// </summary>
public static class DatabaseInitializer
{
    // Demo orders are never part of migrations, so production data stays clean.
    // ORD-404 is intentionally absent ("unknown order" scenario).
    private static readonly (string OrderNumber, string ClientName)[] DemoOrders =
    [
        ("ORD-001", "Alpha Logistics LLP"),
        ("ORD-002", "Beta Construction LLP"),
        ("ORD-003", "Gamma Retail JSC")
    ];

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerSyncDbContext>();

        // In Docker the API starts only after PostgreSQL is healthy, so no retry loop is needed.
        if (options.ApplyMigrationsOnStartup)
            await dbContext.Database.MigrateAsync(cancellationToken);

        if (options.SeedDemoData)
            await SeedDemoOrdersAsync(scope.ServiceProvider, dbContext, cancellationToken);
    }

    /// <summary>
    /// Idempotent: only missing demo orders are added.
    /// </summary>
    private static async Task SeedDemoOrdersAsync(
        IServiceProvider services,
        LedgerSyncDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        var existing = await dbContext.Orders.Select(o => o.OrderNumber).ToListAsync(cancellationToken);
        var missing = DemoOrders
            .Where(o => !existing.Contains(o.OrderNumber))
            .Select(o => Order.Create(o.OrderNumber, o.ClientName, timeProvider.GetUtcNow()))
            .ToList();

        if (missing.Count == 0)
            return;

        dbContext.Orders.AddRange(missing);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Demo data seeded: {Count} orders", missing.Count);
        }
        catch (DbUpdateException ex)
        {
            // Another instance seeded the same orders concurrently; the unique index kept the data consistent.
            logger.LogWarning(ex, "Demo orders were already seeded by another instance");
        }
    }
}
