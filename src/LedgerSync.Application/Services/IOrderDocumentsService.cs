using LedgerSync.Application.Contracts.Orders;

namespace LedgerSync.Application.Services;

/// <summary>
/// Queries documents attached to internal orders.
/// </summary>
public interface IOrderDocumentsService
{
    /// <summary>
    /// Returns documents of the order, or null if the order does not exist.
    /// </summary>
    Task<OrderDocumentsDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken);
}
