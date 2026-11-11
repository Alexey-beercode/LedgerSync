using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Application.Features.SyncLogs.GetSyncLogs;

/// <summary>
/// Latest sync runs, newest first (<c>GET /api/sync/logs</c>).
/// </summary>
public sealed record GetSyncLogsQuery(int Take = GetSyncLogsQuery.DefaultTake) : IQuery<IReadOnlyList<SyncLogHistoryItemDto>>
{
    public const int DefaultTake = 20;
    public const int MaxTake = 100;
}
