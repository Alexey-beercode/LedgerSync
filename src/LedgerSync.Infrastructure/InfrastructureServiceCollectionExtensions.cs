using LedgerSync.Application.Abstractions.Concurrency;
using LedgerSync.Application.Abstractions.ExternalSource;
using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Infrastructure.Concurrency;
using LedgerSync.Infrastructure.ExternalSource;
using LedgerSync.Infrastructure.GoogleSheets;
using LedgerSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LedgerSync.Infrastructure;

/// <summary>
/// Registers infrastructure services: database, external API client, Google Sheets logger.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddExternalSource();
        services.AddGoogleSheets();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>().BindConfiguration(PersistenceOptions.SectionName);

        var connectionString = configuration.GetConnectionString(PersistenceOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{PersistenceOptions.ConnectionStringName}' is not configured.");

        // One shared data source: used by EF Core and by the advisory lock.
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddDbContext<LedgerSyncDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention());

        // Repositories are reached only through the unit of work.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ISyncRunLock, PostgresAdvisorySyncRunLock>();
    }

    private static void AddExternalSource(this IServiceCollection services)
    {
        services.AddOptions<ExternalApiOptions>()
            .BindConfiguration(ExternalApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The standard handler provides timeouts, exponential backoff with jitter and a circuit breaker;
        // only the number of retries and a short base delay are tuned.
        services.AddHttpClient<IExternalDocumentsClient, HttpExternalDocumentsClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value.BaseUrl))
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
            {
                resilience.Retry.MaxRetryAttempts = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value.MaxRetryAttempts;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
            });
    }

    /// <summary>
    /// Mock Google Sheets journal (option C of the assignment). A real integration is another
    /// <see cref="IGoogleSheetLogger"/> implementation registered here instead.
    /// </summary>
    private static void AddGoogleSheets(this IServiceCollection services)
    {
        services.AddOptions<GoogleSheetsOptions>()
            .BindConfiguration(GoogleSheetsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IGoogleSheetLogger, FileGoogleSheetLogger>();
    }
}
