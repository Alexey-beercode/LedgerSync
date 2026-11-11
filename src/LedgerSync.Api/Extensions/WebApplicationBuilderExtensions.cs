using System.Diagnostics;
using LedgerSync.Api.ExceptionHandling;
using LedgerSync.Api.MockExternalApi;
using LedgerSync.Application;
using LedgerSync.Application.Common.Serialization;
using LedgerSync.Infrastructure;

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
        builder.AddMockExternalApi();

        return builder;
    }

    private static void AddPresentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter()));

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

    private static void AddMockExternalApi(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(MockExternalApiOptions.SectionName).Get<MockExternalApiOptions>()
                      ?? new MockExternalApiOptions();

        builder.Services.AddSingleton(options);
        if (options.Enabled)
            builder.Services.AddSingleton(new MockExternalScenarioState(options.Scenario));
    }
}
