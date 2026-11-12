using FluentValidation;
using LedgerSync.Application.Abstractions.Messaging;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Application.Mappings;

namespace LedgerSync.Application.Features.Orders.GetOrderDocuments;

/// <summary>
/// Returns documents of an existing order.
/// </summary>
public sealed class GetOrderDocumentsQueryHandler(
    IUnitOfWork unitOfWork,
    IValidator<GetOrderDocumentsQuery> validator) : IQueryHandler<GetOrderDocumentsQuery, OrderDocumentsDto>
{
    /// <exception cref="OrderNotFoundException">Order does not exist.</exception>
    public async Task<OrderDocumentsDto> HandleAsync(GetOrderDocumentsQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var orderNumber = query.OrderNumber.Trim();
        var order = await unitOfWork.Orders.GetByNumberAsync(orderNumber, cancellationToken)
                    ?? throw new OrderNotFoundException(orderNumber);

        var documents = await unitOfWork.Documents.GetByOrderNumberAsync(order.OrderNumber, cancellationToken);
        return new OrderDocumentsDto(order.OrderNumber, documents.Select(d => d.ToDto()).ToList());
    }
}
