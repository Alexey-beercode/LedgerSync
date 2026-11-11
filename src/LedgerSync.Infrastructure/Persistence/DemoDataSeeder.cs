using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// Inserts demo orders for local and Docker runs. Never part of migrations, so production data stays clean.
/// Idempotent: only missing orders are added. ORD-404 is intentionally absent ("unknown order" scenario).
/// </summary>
public static class DemoDataSeeder
{
    private static readonly (string OrderNumber, string ClientName)[] DemoOrders =
    [
        ("ORD-001", "Alpha Logistics LLP"),
        ("ORD-002", "Beta Construction LLP"),
        ("ORD-003", "Gamma Retail JSC")
    ];

    public static async Task SeedDemoDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerSyncDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DemoDataSeeder));

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
