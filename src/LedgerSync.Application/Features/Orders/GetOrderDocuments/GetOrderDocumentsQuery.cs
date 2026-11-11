using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Contracts.Orders;

namespace LedgerSync.Application.Features.Orders.GetOrderDocuments;

/// <summary>
/// Documents attached to an order (<c>GET /api/orders/{orderNumber}/documents</c>).
/// </summary>
public sealed record GetOrderDocumentsQuery(string OrderNumber) : IQuery<OrderDocumentsDto>;
