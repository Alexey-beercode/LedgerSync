using LedgerSync.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

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
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.MapControllers();

        return app;
    }
}
