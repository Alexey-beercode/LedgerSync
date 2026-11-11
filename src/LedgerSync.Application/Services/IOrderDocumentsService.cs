using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.Orders;

namespace LedgerSync.Application.Services;

/// <summary>
/// Queries documents attached to internal orders.
/// </summary>
public interface IOrderDocumentsService
{
    /// <summary>Returns documents of the order.</summary>
    /// <exception cref="OrderNotFoundException">Order does not exist.</exception>
    Task<OrderDocumentsDto> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken);
}
