using System.Diagnostics;
using LedgerSync.Api.ExceptionHandling;
using LedgerSync.Api.MockExternalApi;
using LedgerSync.Application;
using LedgerSync.Infrastructure;
using LedgerSync.Infrastructure.Persistence;

namespace LedgerSync.Api.Extensions;

/// <summary>
/// Service registration for the host, grouped by layer.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddLedgerSyncServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.AddPresentation();
        builder.AddHealthChecks();
        builder.AddMockExternalApi();

        return builder;
    }

    private static void AddPresentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();

        // Every error response carries a traceId to correlate it with logs.
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddOpenApi();
    }

    private static void AddHealthChecks(this WebApplicationBuilder builder) =>
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<LedgerSyncDbContext>("database", tags: [HealthTags.Ready]);

    private static void AddMockExternalApi(this WebApplicationBuilder builder)
    {
        var initial = builder.Configuration.GetValue("MockExternalApi:Scenario", MockExternalScenario.Baseline);
        builder.Services.AddSingleton(new MockExternalScenarioState(initial));
    }
}

/// <summary>
/// Health check tags.
/// </summary>
internal static class HealthTags
{
    /// <summary>Dependencies required to serve traffic (database).</summary>
    public const string Ready = "ready";
}
