using LedgerSync.Domain.Enums;

namespace LedgerSync.Application.Contracts.Orders;

/// <summary>
/// Response of <c>GET /api/orders/{orderNumber}/documents</c>.
/// </summary>
public sealed record OrderDocumentsDto(
    string OrderNumber,
    IReadOnlyList<DocumentDto> Documents);

/// <summary>
/// Document attached to an order.
/// </summary>
public sealed record DocumentDto(
    string ExternalId,
    DocumentType DocumentType,
    string DocumentNumber,
    decimal Amount,
    string Currency,
    DocumentStatus Status,
    DateTimeOffset ExternalUpdatedAt);
