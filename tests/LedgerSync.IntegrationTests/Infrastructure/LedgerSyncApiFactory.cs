using LedgerSync.Application.Abstractions.ExternalSource;
using LedgerSync.Application.Abstractions.GoogleSheets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API in memory against the test PostgreSQL container, in the "Production" environment,
/// so every demo feature has to be switched on explicitly, exactly as in docker-compose.
/// The sync handler still calls the mock external API over HTTP, routed into the test server.
/// </summary>
public sealed class LedgerSyncApiFactory(
    string connectionString,
    bool googleSheetsDown = false,
    bool mockExternalApiEnabled = true) : WebApplicationFactory<Program>
{
    public string SheetPath { get; } = Path.Combine(Path.GetTempPath(), $"ledgersync-{Guid.NewGuid():N}.csv");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:LedgerSync", connectionString);
        builder.UseSetting("Persistence:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Persistence:SeedDemoData", "true");
        builder.UseSetting("MockExternalApi:Enabled", mockExternalApiEnabled.ToString());
        builder.UseSetting("ExternalApi:BaseUrl", "http://localhost");
        builder.UseSetting("ExternalApi:MaxRetryAttempts", "1");
        builder.UseSetting("GoogleSheets:FilePath", SheetPath);

        builder.ConfigureTestServices(services =>
        {
            services.Configure<HttpClientFactoryOptions>(nameof(IExternalDocumentsClient), options =>
                options.HttpMessageHandlerBuilderActions.Add(handler => handler.PrimaryHandler = Server.CreateHandler()));

            if (googleSheetsDown)
                services.AddSingleton<IGoogleSheetLogger, UnavailableGoogleSheetLogger>();
        });
    }
}
