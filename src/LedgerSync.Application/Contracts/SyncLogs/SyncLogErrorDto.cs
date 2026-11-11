namespace LedgerSync.Application.Contracts.SyncLogs;

/// <summary>
/// Problem with a single document within a sync run.
/// </summary>
public sealed record SyncLogErrorDto(string? ExternalId, string Reason);
