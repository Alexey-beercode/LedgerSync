using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Application.Features.SyncLogs.ResendLatestSyncLog;

/// <summary>
/// Re-sends the latest sync run to Google Sheets (<c>POST /api/sync/logs/latest/send-to-google</c>).
/// Idempotent: the sheet row is upserted by RunId.
/// </summary>
public sealed record ResendLatestSyncLogCommand : ICommand<GoogleSheetResendResultDto>;
