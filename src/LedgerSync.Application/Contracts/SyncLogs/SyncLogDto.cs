using LedgerSync.Domain.Enums;

namespace LedgerSync.Application.Contracts.SyncLogs;

/// <summary>
/// One sync run as a Google Sheets row. Field order matches the sheet columns.
/// </summary>
public sealed record SyncLogDto(
    Guid RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    SyncStatus Status,
    int DocumentsReceived,
    int DocumentsCreated,
    int DocumentsUpdated,
    int DocumentsSkipped,
    string? ErrorMessage);
