using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;

namespace LedgerSync.UnitTests.Fakes;

/// <summary>
/// Google Sheets logger spy: records sent rows, can simulate an outage.
/// </summary>
internal sealed class RecordingGoogleSheetLogger : IGoogleSheetLogger
{
    public List<SyncLogDto> SentRows { get; } = [];

    public bool IsUnavailable { get; set; }

    public Task SendSyncLogAsync(SyncLogDto syncLog, CancellationToken cancellationToken)
    {
        if (IsUnavailable)
            throw new GoogleSheetDeliveryException("Google Sheets is down (test).");

        SentRows.Add(syncLog);
        return Task.CompletedTask;
    }
}
