using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Application.Features.SyncLogs.ResendSyncLog;

/// <summary>
/// Re-sends a sync run to Google Sheets: the given one, or the latest when <see cref="RunId"/> is null.
/// Idempotent: the sheet row is upserted by RunId.
/// </summary>
public sealed record ResendSyncLogCommand(Guid? RunId = null) : ICommand<GoogleSheetResendResultDto>;
