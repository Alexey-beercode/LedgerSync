using LedgerSync.Application.Contracts.ExternalSource;
using LedgerSync.Domain.Enumerations;

namespace LedgerSync.Application.Features.Sync.SyncDocuments;

/// <summary>
/// External document after validation: all fields are present and strongly typed.
/// </summary>
internal sealed record ValidatedDocument(
    string ExternalId,
    string OrderNumber,
    DocumentType DocumentType,
    string DocumentNumber,
    decimal Amount,
    string Currency,
    DocumentStatus Status,
    DateTimeOffset ExternalUpdatedAt)
{
    /// <summary>Converts a payload that already passed validation.</summary>
    public static ValidatedDocument From(ExternalDocumentPayload payload) => new(
        payload.ExternalId!.Trim(),
        payload.OrderNumber!.Trim(),
        DocumentType.FromName(payload.DocumentType!),
        payload.DocumentNumber!.Trim(),
        payload.Amount!.Value,
        payload.Currency!.Trim(),
        DocumentStatus.FromName(payload.Status!),
        payload.ExternalUpdatedAt!.Value.ToUniversalTime());
}
