namespace LedgerSync.Application.Contracts.Orders;

/// <summary>
/// Document attached to an order.
/// </summary>
public sealed record DocumentDto(
    string ExternalId,
    string DocumentType,
    string DocumentNumber,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset ExternalUpdatedAt);
