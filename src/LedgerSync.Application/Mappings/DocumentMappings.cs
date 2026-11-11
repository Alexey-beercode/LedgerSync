using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Domain.Entities;

namespace LedgerSync.Application.Mappings;

/// <summary>
/// Explicit mappings from <see cref="ExternalDocument"/> to contracts.
/// </summary>
public static class DocumentMappings
{
    public static DocumentDto ToDto(this ExternalDocument document) => new(
        document.ExternalId,
        document.DocumentType.Name,
        document.DocumentNumber,
        document.Amount,
        document.Currency,
        document.Status.Name,
        document.ExternalUpdatedAt);
}
