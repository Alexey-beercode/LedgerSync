using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// Production host with only a connection string: external API address and Google mode are missing.
/// </summary>
public sealed class UnconfiguredApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:LedgerSync", connectionString);
    }
}
