using LedgerSync.Application;
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
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        return builder;
    }
}
