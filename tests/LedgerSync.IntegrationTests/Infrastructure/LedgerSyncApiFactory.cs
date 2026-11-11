using LedgerSync.Application.Abstractions.ExternalSource;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API in memory against the test PostgreSQL container.
/// The sync service still calls the mock external API over HTTP, routed into the test server.
/// </summary>
public sealed class LedgerSyncApiFactory(string connectionString, string googleSheetsMode) : WebApplicationFactory<Program>
{
    private readonly string _sheetPath = Path.Combine(Path.GetTempPath(), $"ledgersync-{Guid.NewGuid():N}.csv");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LedgerSync", connectionString);
        builder.UseSetting("Persistence:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("ExternalApi:BaseUrl", "http://localhost");
        builder.UseSetting("ExternalApi:MaxRetryAttempts", "1");
        builder.UseSetting("GoogleSheets:Mode", googleSheetsMode);
        builder.UseSetting("GoogleSheets:FilePath", _sheetPath);

        builder.ConfigureTestServices(services =>
            services.Configure<HttpClientFactoryOptions>(nameof(IExternalDocumentsClient), options =>
                options.HttpMessageHandlerBuilderActions.Add(handler => handler.PrimaryHandler = Server.CreateHandler())));
    }

    public string SheetPath => _sheetPath;
}
