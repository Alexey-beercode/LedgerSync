using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Logging;
using LedgerSync.Application.Mappings;
using LedgerSync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LedgerSync.Application.Features.Sync;

/// <summary>
/// Sends a finished run to Google Sheets and stores the delivery result.
/// Never throws: Google problems must not affect already saved documents.
/// </summary>
public sealed class GoogleSheetReporter(
    IGoogleSheetLogger googleSheetLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<GoogleSheetReporter> logger)
{
    public async Task ReportAsync(SyncLog syncLog, CancellationToken cancellationToken)
    {
        try
        {
            await googleSheetLogger.SendSyncLogAsync(syncLog.ToGoogleRow(), cancellationToken);
            syncLog.MarkGoogleSheetSent(timeProvider.GetUtcNow());
            logger.GoogleSheetSent(syncLog.Id);
        }
        catch (Exception ex)
        {
            // Any failure (network, script error, bug in an implementation) is recorded, not propagated.
            syncLog.MarkGoogleSheetFailed(ex.Message);
            logger.GoogleSheetFailed(ex, syncLog.Id);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Documents and the run itself are already committed; only the delivery flag is lost.
            logger.GoogleSheetStatusSaveFailed(ex, syncLog.Id);
        }
    }
}
