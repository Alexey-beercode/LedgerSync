using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations. In Docker the API starts only after PostgreSQL is healthy,
/// so no retry loop is needed here.
/// </summary>
public static class DatabaseMigrator
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LedgerSyncDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
