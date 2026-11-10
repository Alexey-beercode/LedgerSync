using LedgerSync.Domain.Entities;

namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Read access to internal orders.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Loads orders by their numbers in a single query. Key is the order number.
    /// </summary>
    Task<IReadOnlyDictionary<string, Order>> GetByNumbersAsync(
        IReadOnlyCollection<string> orderNumbers,
        CancellationToken cancellationToken);
}
