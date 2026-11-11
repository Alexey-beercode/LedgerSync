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
public static class DependencyInjection
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
        services.AddOptions<PersistenceOptions>()
            .BindConfiguration(PersistenceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString(PersistenceOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{PersistenceOptions.ConnectionStringName}' is not configured.");

        // One shared data source: used by EF Core and by the advisory lock.
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddDbContext<LedgerSyncDbContext>((sp, options) =>
        {
            var persistence = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options
                .UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(),
                    npgsql => npgsql.CommandTimeout(persistence.CommandTimeoutSeconds))
                .UseSnakeCaseNamingConvention();
        });

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
            {
                var options = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.MaxResponseContentBufferSize = options.MaxResponseSizeMb * 1024L * 1024L;
                client.Timeout = Timeout.InfiniteTimeSpan; // Timeouts are owned by the resilience pipeline.
            })
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
            {
                var options = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value;
                ConfigureResilience(resilience, options.AttemptTimeoutSeconds, options.MaxRetryAttempts);
            });
    }

    private static void AddGoogleSheets(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleSheetsOptions>()
            .BindConfiguration(GoogleSheetsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var mode = configuration.GetSection(GoogleSheetsOptions.SectionName)
            .GetValue(nameof(GoogleSheetsOptions.Mode), GoogleSheetsMode.File);

        if (mode == GoogleSheetsMode.File)
        {
            services.AddSingleton<IGoogleSheetLogger, FileGoogleSheetLogger>();
            return;
        }

        services.AddHttpClient<IGoogleSheetLogger, AppsScriptGoogleSheetLogger>(client =>
                client.Timeout = Timeout.InfiniteTimeSpan)
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
            {
                var options = sp.GetRequiredService<IOptions<GoogleSheetsOptions>>().Value;
                ConfigureResilience(resilience, options.AttemptTimeoutSeconds, options.MaxRetryAttempts);
            });
    }

    /// <summary>
    /// Retry with exponential backoff and jitter, per-attempt timeout, total timeout and circuit breaker.
    /// </summary>
    private static void ConfigureResilience(HttpStandardResilienceOptions resilience, int attemptTimeoutSeconds, int maxRetries)
    {
        var attemptTimeout = TimeSpan.FromSeconds(attemptTimeoutSeconds);

        resilience.AttemptTimeout.Timeout = attemptTimeout;
        resilience.Retry.MaxRetryAttempts = maxRetries;
        resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
        resilience.Retry.UseJitter = true;
        resilience.TotalRequestTimeout.Timeout = attemptTimeout * (maxRetries + 1) + TimeSpan.FromSeconds(10);
        resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2 + TimeSpan.FromSeconds(1);
    }
}
