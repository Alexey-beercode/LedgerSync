namespace LedgerSync.Application.Contracts.SyncLogs;

/// <summary>
/// Response of <c>POST /api/sync/logs/latest/send-to-google</c>.
/// </summary>
public sealed record GoogleSheetResendResultDto(
    Guid RunId,
    string GoogleSheetStatus,
    string? GoogleSheetError);
