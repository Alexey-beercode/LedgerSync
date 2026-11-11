using Microsoft.Extensions.Logging;

namespace LedgerSync.Application.Logging;

/// <summary>
/// Structured log events of the sync process (source-generated, allocation-free).
/// Event ids are stable so they can be used in log queries and alerts.
/// </summary>
internal static partial class SyncLogMessages
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Sync run {RunId} started")]
    public static partial void SyncStarted(this ILogger logger, Guid runId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Sync run {RunId} finished with {Status}: received {Received}, created {Created}, updated {Updated}, skipped {Skipped} in {ElapsedMs} ms")]
    public static partial void SyncFinished(this ILogger logger, Guid runId, string status,
        int received, int created, int updated, int skipped, long elapsedMs);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Sync rejected: another run is already in progress")]
    public static partial void SyncAlreadyRunning(this ILogger logger);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Sync run {RunId} was left unfinished and is marked as Failed")]
    public static partial void InterruptedRunRecovered(this ILogger logger, Guid runId);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Error, Message = "External source failed during sync run {RunId}")]
    public static partial void ExternalSourceFailed(this ILogger logger, Exception exception, Guid runId);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning, Message = "Document {ExternalId} rejected: {Reason}")]
    public static partial void DocumentRejected(this ILogger logger, string? externalId, string reason);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning,
        Message = "Document {ExternalId} ignored: incoming version {IncomingUpdatedAt:o} is older than stored {StoredUpdatedAt:o}")]
    public static partial void StaleDocumentIgnored(this ILogger logger, string externalId,
        DateTimeOffset incomingUpdatedAt, DateTimeOffset storedUpdatedAt);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning,
        Message = "Document {ExternalId} appears {Count} times in one batch; only the latest version is used")]
    public static partial void DuplicateInBatch(this ILogger logger, string externalId, int count);

    [LoggerMessage(EventId = 1200, Level = LogLevel.Error, Message = "Processing or saving documents of sync run {RunId} failed; all changes were rolled back")]
    public static partial void DatabaseSaveFailed(this ILogger logger, Exception exception, Guid runId);

    [LoggerMessage(EventId = 1300, Level = LogLevel.Information, Message = "Sync run {RunId} sent to Google Sheets")]
    public static partial void GoogleSheetSent(this ILogger logger, Guid runId);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Error, Message = "Sending sync run {RunId} to Google Sheets failed")]
    public static partial void GoogleSheetFailed(this ILogger logger, Exception exception, Guid runId);

    [LoggerMessage(EventId = 1302, Level = LogLevel.Error, Message = "Saving Google Sheets status of sync run {RunId} failed")]
    public static partial void GoogleSheetStatusSaveFailed(this ILogger logger, Exception exception, Guid runId);
}
