using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations; does not connect to the database.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LedgerSyncDbContext>
{
    public LedgerSyncDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LedgerSyncDbContext>()
            .UseNpgsql("Host=localhost;Database=ledgersync;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LedgerSyncDbContext(options);
    }
}
