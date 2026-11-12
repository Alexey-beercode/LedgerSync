using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// Simulates a Google Sheets outage in tests.
/// </summary>
internal sealed class UnavailableGoogleSheetLogger : IGoogleSheetLogger
{
    public Task SendSyncLogAsync(SyncLogDto syncLog, CancellationToken cancellationToken) =>
        throw new GoogleSheetDeliveryException("Google Sheets is unavailable (test).");
}
