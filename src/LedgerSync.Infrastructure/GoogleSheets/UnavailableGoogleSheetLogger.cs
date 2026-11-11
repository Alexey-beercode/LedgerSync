using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Demo logger that always fails. Shows that a Google outage never affects saved documents
/// and that the run can be resent later.
/// </summary>
internal sealed class UnavailableGoogleSheetLogger : IGoogleSheetLogger
{
    public Task SendSyncLogAsync(SyncLogDto syncLog, CancellationToken cancellationToken) =>
        throw new GoogleSheetDeliveryException("Google Sheets is unavailable (simulated by GoogleSheets:Mode=Unavailable).");
}
