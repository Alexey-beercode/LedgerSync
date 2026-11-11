using LedgerSync.Application.Contracts.ExternalSource;
using LedgerSync.Application.Common.Exceptions;

namespace LedgerSync.Application.Abstractions.ExternalSource;

/// <summary>
/// Client of the external documents system.
/// </summary>
public interface IExternalDocumentsClient
{
    /// <summary>
    /// Fetches the current list of documents. Items are returned as raw payloads and validated by the caller.
    /// </summary>
    /// <exception cref="ExternalSourceUnavailableException">Source is unreachable or returned an error code.</exception>
    /// <exception cref="ExternalSourceInvalidResponseException">Response body could not be parsed.</exception>
    Task<IReadOnlyList<ExternalDocumentPayload>> FetchDocumentsAsync(CancellationToken cancellationToken);
}
