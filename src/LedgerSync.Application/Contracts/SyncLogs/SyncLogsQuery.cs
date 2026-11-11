namespace LedgerSync.Application.Contracts.SyncLogs;

/// <summary>
/// Parameters of <c>GET /api/sync/logs</c>.
/// </summary>
public sealed record SyncLogsQuery(int Take = SyncLogsQuery.DefaultTake)
{
    public const int DefaultTake = 20;
    public const int MaxTake = 100;
}
