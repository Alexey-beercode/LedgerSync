using LedgerSync.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace LedgerSync.Api.Extensions;

/// <summary>
/// HTTP pipeline and startup tasks.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies EF Core migrations when <c>Persistence:ApplyMigrationsOnStartup</c> is enabled.
    /// </summary>
    public static async Task MigrateDatabaseIfEnabledAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<PersistenceOptions>>().Value;
        if (options.ApplyMigrationsOnStartup)
            await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping);
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

        app.MapHealthEndpoints();
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Liveness: the process responds. Readiness: the database is reachable.
    /// </summary>
    private static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthTags.Ready)
        });
    }
}
