using FluentValidation;
using LedgerSync.Application.Orders;
using LedgerSync.Application.Services;
using LedgerSync.Application.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSync.Application;

/// <summary>
/// Registers use cases and validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<GoogleSheetReporter>();
        services.AddScoped<IDocumentSyncService, DocumentSyncService>();
        services.AddScoped<ISyncLogService, SyncLogService>();
        services.AddScoped<IOrderDocumentsService, OrderDocumentsService>();

        return services;
    }
}
