using FluentValidation;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Application.Mappings;
using LedgerSync.Application.Services;

namespace LedgerSync.Application.Orders;

/// <summary>
/// Returns documents attached to an order.
/// </summary>
public sealed class OrderDocumentsService(
    IOrderRepository orders,
    IExternalDocumentRepository documents,
    IValidator<OrderDocumentsQuery> queryValidator) : IOrderDocumentsService
{
    public async Task<OrderDocumentsDto> GetByOrderNumberAsync(OrderDocumentsQuery query, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var orderNumber = query.OrderNumber.Trim();
        var order = await orders.GetByNumberAsync(orderNumber, cancellationToken)
                    ?? throw new OrderNotFoundException(orderNumber);

        var orderDocuments = await documents.GetByOrderNumberAsync(order.OrderNumber, cancellationToken);
        return new OrderDocumentsDto(order.OrderNumber, orderDocuments.Select(d => d.ToDto()).ToList());
    }
}
