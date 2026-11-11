using LedgerSync.Api.MockExternalApi;
using LedgerSync.Infrastructure.Persistence;
using Scalar.AspNetCore;

namespace LedgerSync.Api.Extensions;

/// <summary>
/// HTTP pipeline and startup tasks.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies migrations and demo data when enabled in <c>Persistence</c> settings.
    /// </summary>
    public static Task InitializeDatabaseAsync(this WebApplication app) =>
        app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);

    public static WebApplication UseLedgerSyncPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // The API contract is not secret, so OpenAPI and Scalar UI are always available.
        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithTitle("LedgerSync API"));

        if (app.Services.GetRequiredService<MockExternalApiOptions>().Enabled)
            app.MapMockExternalApi();

        app.MapControllers();

        return app;
    }
}
