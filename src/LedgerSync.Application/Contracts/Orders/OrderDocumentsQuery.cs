namespace LedgerSync.Application.Contracts.Orders;

/// <summary>
/// Parameters of <c>GET /api/orders/{orderNumber}/documents</c>.
/// </summary>
public sealed record OrderDocumentsQuery(string OrderNumber);
