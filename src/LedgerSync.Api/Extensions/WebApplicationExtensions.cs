using LedgerSync.Api.MockExternalApi;
using LedgerSync.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace LedgerSync.Api.Extensions;

/// <summary>
/// HTTP pipeline and startup tasks.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies migrations (<c>Persistence:ApplyMigrationsOnStartup</c>)
    /// and demo data (<c>Persistence:SeedDemoData</c>) when enabled.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        var stopping = app.Lifetime.ApplicationStopping;

        if (options.ApplyMigrationsOnStartup)
            await app.Services.MigrateDatabaseAsync(stopping);

        if (options.SeedDemoData)
            await app.Services.SeedDemoDataAsync(stopping);
    }

    public static WebApplication UseLedgerSyncPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options.WithTitle("LedgerSync API"));
        }

        if (app.Services.GetRequiredService<MockExternalApiOptions>().Enabled)
            app.MapMockExternalApi();

        app.MapControllers();

        return app;
    }
}
