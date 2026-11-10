using LedgerSync.Domain.Enums;

namespace LedgerSync.Application.Contracts.SyncLogs;

/// <summary>
/// Sync run as returned by <c>GET /api/sync/logs</c>, including Google delivery state and document errors.
/// </summary>
public sealed record SyncLogHistoryItemDto(
    Guid RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    SyncStatus Status,
    int DocumentsReceived,
    int DocumentsCreated,
    int DocumentsUpdated,
    int DocumentsSkipped,
    string? ErrorMessage,
    GoogleSheetStatus GoogleSheetStatus,
    string? GoogleSheetError,
    IReadOnlyList<SyncLogErrorDto> Errors);

/// <summary>
/// Problem with a single document within a sync run.
/// </summary>
public sealed record SyncLogErrorDto(string? ExternalId, string Reason);
