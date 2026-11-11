using FluentValidation;
using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Features.Orders.GetOrderDocuments;
using LedgerSync.Application.Features.Sync;
using LedgerSync.Application.Features.Sync.SyncDocuments;
using LedgerSync.Application.Features.SyncLogs.GetSyncLogs;
using LedgerSync.Application.Features.SyncLogs.ResendSyncLog;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSync.Application;

/// <summary>
/// Registers command/query handlers and validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Error messages are stored in SyncLog and returned by the API: keep them in English regardless of server locale.
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);
        services.AddScoped<GoogleSheetReporter>();

        // Commands
        services.AddScoped<ICommandHandler<SyncDocumentsCommand, SyncResultDto>, SyncDocumentsCommandHandler>();
        services.AddScoped<ICommandHandler<ResendSyncLogCommand, GoogleSheetResendResultDto>, ResendSyncLogCommandHandler>();

        // Queries
        services.AddScoped<IQueryHandler<GetSyncLogsQuery, IReadOnlyList<SyncLogHistoryItemDto>>, GetSyncLogsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderDocumentsQuery, OrderDocumentsDto>, GetOrderDocumentsQueryHandler>();

        return services;
    }
}
