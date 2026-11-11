using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Features.Sync;
using LedgerSync.Application.Mappings;

namespace LedgerSync.Application.Features.SyncLogs.ResendLatestSyncLog;

/// <summary>
/// Delivers the latest finished run to Google Sheets again.
/// </summary>
public sealed class ResendLatestSyncLogCommandHandler(
    IUnitOfWork unitOfWork,
    GoogleSheetReporter googleSheetReporter) : ICommandHandler<ResendLatestSyncLogCommand, GoogleSheetResendResultDto>
{
    /// <exception cref="SyncRunNotFoundException">There are no runs yet.</exception>
    /// <exception cref="SyncAlreadyRunningException">The latest run is still in progress.</exception>
    public async Task<GoogleSheetResendResultDto> HandleAsync(ResendLatestSyncLogCommand command, CancellationToken cancellationToken)
    {
        var latest = await unitOfWork.SyncLogs.GetLatestAsync(cancellationToken) ?? throw new SyncRunNotFoundException();

        // A running run has no final numbers yet; sending it would publish a misleading row.
        if (!latest.IsFinished)
            throw new SyncAlreadyRunningException();

        await googleSheetReporter.ReportAsync(latest, CancellationToken.None);
        return latest.ToResendResult();
    }
}
