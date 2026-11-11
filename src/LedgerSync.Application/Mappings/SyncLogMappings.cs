using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Domain.Entities;

namespace LedgerSync.Application.Mappings;

/// <summary>
/// Explicit mappings from <see cref="SyncLog"/> to contracts.
/// </summary>
public static class SyncLogMappings
{
    public static SyncResultDto ToSyncResult(this SyncLog log) => new(
        log.Id,
        log.Status.Name,
        log.DocumentsReceived,
        log.DocumentsCreated,
        log.DocumentsUpdated,
        log.DocumentsSkipped,
        log.ErrorMessage,
        log.GoogleSheetStatus.Name);

    /// <summary>Row for the Google Sheets journal.</summary>
    public static SyncLogDto ToGoogleRow(this SyncLog log) => new(
        log.Id,
        log.StartedAt,
        log.FinishedAt,
        log.Status.Name,
        log.DocumentsReceived,
        log.DocumentsCreated,
        log.DocumentsUpdated,
        log.DocumentsSkipped,
        log.ErrorMessage);

    public static SyncLogHistoryItemDto ToHistoryItem(this SyncLog log) => new(
        log.Id,
        log.StartedAt,
        log.FinishedAt,
        log.Status.Name,
        log.DocumentsReceived,
        log.DocumentsCreated,
        log.DocumentsUpdated,
        log.DocumentsSkipped,
        log.ErrorMessage,
        log.GoogleSheetStatus.Name,
        log.GoogleSheetError,
        log.Errors.Select(e => new SyncLogErrorDto(e.ExternalId, e.Reason)).ToList());

    public static GoogleSheetResendResultDto ToResendResult(this SyncLog log) => new(
        log.Id,
        log.GoogleSheetStatus.Name,
        log.GoogleSheetError);
}
