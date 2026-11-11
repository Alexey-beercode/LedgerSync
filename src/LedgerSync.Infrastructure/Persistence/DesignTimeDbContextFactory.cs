using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations. Never connects, so no credentials are needed.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LedgerSyncDbContext>
{
    public LedgerSyncDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LedgerSyncDbContext>()
            .UseNpgsql("Host=localhost;Database=ledgersync")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LedgerSyncDbContext(options);
    }
}
