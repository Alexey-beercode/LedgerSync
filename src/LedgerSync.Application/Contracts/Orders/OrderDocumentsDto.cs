namespace LedgerSync.Application.Contracts.Orders;

/// <summary>
/// Response of <c>GET /api/orders/{orderNumber}/documents</c>.
/// </summary>
public sealed record OrderDocumentsDto(
    string OrderNumber,
    IReadOnlyList<DocumentDto> Documents);
