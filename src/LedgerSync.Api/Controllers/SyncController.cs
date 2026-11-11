using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Application.Features.Sync.SyncDocuments;
using LedgerSync.Application.Features.SyncLogs;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSync.Api.Controllers;

/// <summary>
/// Document synchronization and its history. Each action resolves only the handler it needs.
/// </summary>
[ApiController]
[Route("api/sync")]
[Produces("application/json")]
public sealed class SyncController : ControllerBase
{
    /// <summary>Runs synchronization with the external system.</summary>
    /// <remarks>
    /// Returns 200 with the run result even if the run failed (status Failed): the failure is a recorded outcome.
    /// Returns 409 if another run is in progress.
    /// </remarks>
    [HttpPost("documents")]
    [ProducesResponseType<SyncResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SyncResultDto>> RunSync(
        [FromServices] ICommandHandler<SyncDocumentsCommand, SyncResultDto> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new SyncDocumentsCommand(), cancellationToken));

    /// <summary>Returns the latest sync runs, newest first.</summary>
    [HttpGet("logs")]
    [ProducesResponseType<IReadOnlyList<SyncLogHistoryItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SyncLogHistoryItemDto>>> GetLogs(
        [FromServices] IQueryHandler<GetSyncLogsQuery, IReadOnlyList<SyncLogHistoryItemDto>> handler,
        [FromQuery] int take = GetSyncLogsQuery.DefaultTake,
        CancellationToken cancellationToken = default) =>
        Ok(await handler.HandleAsync(new GetSyncLogsQuery(take), cancellationToken));

    /// <summary>Re-sends the latest sync run to Google Sheets.</summary>
    /// <remarks>Idempotent: the sheet row is upserted by RunId, so repeated calls never duplicate it.</remarks>
    [HttpPost("logs/latest/send-to-google")]
    [ProducesResponseType<GoogleSheetResendResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GoogleSheetResendResultDto>> ResendLatestToGoogle(
        [FromServices] ICommandHandler<ResendLatestSyncLogCommand, GoogleSheetResendResultDto> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new ResendLatestSyncLogCommand(), cancellationToken));
}
