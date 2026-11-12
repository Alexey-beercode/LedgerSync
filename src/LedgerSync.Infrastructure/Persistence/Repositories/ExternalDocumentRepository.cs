using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerSync.Infrastructure.Persistence.Repositories;

internal sealed class ExternalDocumentRepository(LedgerSyncDbContext dbContext) : IExternalDocumentRepository
{
    public async Task<IReadOnlyDictionary<string, ExternalDocument>> GetByExternalIdsAsync(
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken)
    {
        if (externalIds.Count == 0)
            return new Dictionary<string, ExternalDocument>();

        // Tracked on purpose: returned documents may be updated within the same unit of work.
        return await dbContext.ExternalDocuments
            .Where(d => externalIds.Contains(d.ExternalId))
            .ToDictionaryAsync(d => d.ExternalId, StringComparer.Ordinal, cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalDocument>> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken) =>
        await dbContext.ExternalDocuments
            .AsNoTracking()
            .Where(d => d.OrderNumber == orderNumber)
            .OrderBy(d => d.ExternalUpdatedAt)
            .ThenBy(d => d.ExternalId)
            .ToListAsync(cancellationToken);

    public void Add(ExternalDocument document) => dbContext.ExternalDocuments.Add(document);
}
