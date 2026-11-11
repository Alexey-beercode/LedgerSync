using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSync.Api.Controllers;

/// <summary>
/// Document synchronization and its history.
/// </summary>
[ApiController]
[Route("api/sync")]
[Produces("application/json")]
public sealed class SyncController(IDocumentSyncService syncService, ISyncLogService syncLogService) : ControllerBase
{
    /// <summary>Runs synchronization with the external system.</summary>
    /// <remarks>
    /// Returns 200 with the run result even if the run failed (status Failed): the failure is a recorded outcome.
    /// Returns 409 if another run is in progress.
    /// </remarks>
    [HttpPost("documents")]
    [ProducesResponseType<SyncResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SyncResultDto>> RunSync(CancellationToken cancellationToken) =>
        Ok(await syncService.RunAsync(cancellationToken));

    /// <summary>Returns the latest sync runs, newest first.</summary>
    [HttpGet("logs")]
    [ProducesResponseType<IReadOnlyList<SyncLogHistoryItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SyncLogHistoryItemDto>>> GetLogs(
        [FromQuery] int take = SyncLogsQuery.DefaultTake,
        CancellationToken cancellationToken = default) =>
        Ok(await syncLogService.GetRecentAsync(new SyncLogsQuery(take), cancellationToken));

    /// <summary>Re-sends the latest sync run to Google Sheets.</summary>
    /// <remarks>Idempotent: the sheet row is upserted by RunId, so repeated calls never duplicate it.</remarks>
    [HttpPost("logs/latest/send-to-google")]
    [ProducesResponseType<GoogleSheetResendResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GoogleSheetResendResultDto>> ResendLatestToGoogle(CancellationToken cancellationToken) =>
        Ok(await syncLogService.ResendLatestToGoogleAsync(cancellationToken));
}
