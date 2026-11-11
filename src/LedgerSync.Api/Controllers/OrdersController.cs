using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Application.Features.Orders;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSync.Api.Controllers;

/// <summary>
/// Orders and their synchronized documents.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    /// <summary>Returns documents attached to the order.</summary>
    [HttpGet("{orderNumber}/documents")]
    [ProducesResponseType<OrderDocumentsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDocumentsDto>> GetDocuments(
        string orderNumber,
        [FromServices] IQueryHandler<GetOrderDocumentsQuery, OrderDocumentsDto> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetOrderDocumentsQuery(orderNumber), cancellationToken));
}
