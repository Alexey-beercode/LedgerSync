namespace LedgerSync.Application.Contracts.ExternalSource;

/// <summary>
/// Raw document as returned by the external system.
/// All fields are nullable/strings on purpose: the payload is untrusted and validated before mapping.
/// </summary>
public sealed record ExternalDocumentPayload(
    string? ExternalId,
    string? OrderNumber,
    string? DocumentType,
    string? DocumentNumber,
    decimal? Amount,
    string? Currency,
    string? Status,
    DateTimeOffset? ExternalUpdatedAt);
