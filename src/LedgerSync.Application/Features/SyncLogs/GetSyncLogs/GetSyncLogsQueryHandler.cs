using FluentValidation;
using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Mappings;

namespace LedgerSync.Application.Features.SyncLogs.GetSyncLogs;

/// <summary>
/// Returns the latest sync runs with their per-document errors.
/// </summary>
public sealed class GetSyncLogsQueryHandler(
    IUnitOfWork unitOfWork,
    IValidator<GetSyncLogsQuery> validator) : IQueryHandler<GetSyncLogsQuery, IReadOnlyList<SyncLogHistoryItemDto>>
{
    public async Task<IReadOnlyList<SyncLogHistoryItemDto>> HandleAsync(GetSyncLogsQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var runs = await unitOfWork.SyncLogs.GetRecentAsync(query.Take, cancellationToken);
        return runs.Select(r => r.ToHistoryItem()).ToList();
    }
}
