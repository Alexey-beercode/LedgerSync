namespace LedgerSync.Application.Contracts.SyncLogs;

/// <summary>
/// Sync run as returned by <c>GET /api/sync/logs</c>, including Google delivery state and document errors.
/// </summary>
public sealed record SyncLogHistoryItemDto(
    Guid RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string Status,
    int DocumentsReceived,
    int DocumentsCreated,
    int DocumentsUpdated,
    int DocumentsSkipped,
    string? ErrorMessage,
    string GoogleSheetStatus,
    string? GoogleSheetError,
    IReadOnlyList<SyncLogErrorDto> Errors);
