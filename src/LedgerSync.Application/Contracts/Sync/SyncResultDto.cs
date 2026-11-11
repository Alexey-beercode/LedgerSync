namespace LedgerSync.Application.Contracts.Sync;

/// <summary>
/// Response of <c>POST /api/sync/documents</c>.
/// </summary>
public sealed record SyncResultDto(
    Guid RunId,
    string Status,
    int DocumentsReceived,
    int DocumentsCreated,
    int DocumentsUpdated,
    int DocumentsSkipped,
    string? ErrorMessage,
    string GoogleSheetStatus);
