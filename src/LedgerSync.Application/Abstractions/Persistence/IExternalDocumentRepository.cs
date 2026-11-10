using LedgerSync.Domain.Entities;

namespace LedgerSync.Application.Abstractions.Persistence;

/// <summary>
/// Storage of documents received from the external system.
/// </summary>
public interface IExternalDocumentRepository
{
    /// <summary>
    /// Loads already known documents by external ids in a single query. Key is the external id.
    /// </summary>
    Task<IReadOnlyDictionary<string, ExternalDocument>> GetByExternalIdsAsync(
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExternalDocument>> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken);

    void Add(ExternalDocument document);
}
