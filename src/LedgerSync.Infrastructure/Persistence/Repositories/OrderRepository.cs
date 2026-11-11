using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerSync.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(LedgerSyncDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken) =>
        dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<IReadOnlyDictionary<string, Order>> GetByNumbersAsync(
        IReadOnlyCollection<string> orderNumbers,
        CancellationToken cancellationToken)
    {
        if (orderNumbers.Count == 0)
            return new Dictionary<string, Order>();

        return await dbContext.Orders
            .AsNoTracking()
            .Where(o => orderNumbers.Contains(o.OrderNumber))
            .ToDictionaryAsync(o => o.OrderNumber, StringComparer.Ordinal, cancellationToken);
    }
}
