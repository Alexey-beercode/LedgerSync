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
        services.AddGoogleSheets(configuration);
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

        services.AddHttpClient<IExternalDocumentsClient, HttpExternalDocumentsClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value.BaseUrl))
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
                ConfigureRetries(resilience, sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value.MaxRetryAttempts));
    }

    private static void AddGoogleSheets(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleSheetsOptions>()
            .BindConfiguration(GoogleSheetsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // A missing mode is reported by ValidateOnStart; the registration below just has to pick something.
        var mode = configuration.GetSection(GoogleSheetsOptions.SectionName)
            .GetValue<GoogleSheetsMode?>(nameof(GoogleSheetsOptions.Mode));

        if (mode != GoogleSheetsMode.AppsScript)
        {
            services.AddSingleton<IGoogleSheetLogger, FileGoogleSheetLogger>();
            return;
        }

        services.AddHttpClient<IGoogleSheetLogger, AppsScriptGoogleSheetLogger>()
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
                ConfigureRetries(resilience, sp.GetRequiredService<IOptions<GoogleSheetsOptions>>().Value.MaxRetryAttempts));
    }

    /// <summary>
    /// The standard handler already provides timeouts, exponential backoff with jitter and a circuit breaker;
    /// only the number of retries and a short base delay are tuned here.
    /// </summary>
    private static void ConfigureRetries(HttpStandardResilienceOptions resilience, int maxRetries)
    {
        resilience.Retry.MaxRetryAttempts = maxRetries;
        resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
    }
}
