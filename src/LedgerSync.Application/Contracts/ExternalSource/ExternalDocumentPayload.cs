using System.Text.Json.Serialization;

namespace LedgerSync.Application.Contracts.ExternalSource;

/// <summary>
/// Raw document as returned by the external system.
/// All fields are nullable on purpose: the payload is untrusted and validated before mapping.
/// </summary>
public sealed record ExternalDocumentPayload(
    string? ExternalId,
    string? OrderNumber,
    string? DocumentType,
    string? DocumentNumber,
    decimal? Amount,
    string? Currency,
    string? Status,
    DateTimeOffset? ExternalUpdatedAt)
{
    /// <summary>
    /// Set when the item could not be read at all (e.g. a number field contains text).
    /// Such an item is rejected individually instead of failing the whole run.
    /// </summary>
    [JsonIgnore]
    public string? ReadError { get; init; }

    public static ExternalDocumentPayload Unreadable(string? externalId, string error) =>
        new(externalId, null, null, null, null, null, null, null) { ReadError = error };
}
