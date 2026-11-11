using FluentValidation;
using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Mappings;

namespace LedgerSync.Application.Features.SyncLogs;

/// <summary>
/// Latest sync runs, newest first (<c>GET /api/sync/logs</c>).
/// </summary>
public sealed record GetSyncLogsQuery(int Take = GetSyncLogsQuery.DefaultTake) : IQuery<IReadOnlyList<SyncLogHistoryItemDto>>
{
    public const int DefaultTake = 20;
    public const int MaxTake = 100;
}

public sealed class GetSyncLogsQueryValidator : AbstractValidator<GetSyncLogsQuery>
{
    public GetSyncLogsQueryValidator() =>
        RuleFor(q => q.Take).InclusiveBetween(1, GetSyncLogsQuery.MaxTake);
}

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
