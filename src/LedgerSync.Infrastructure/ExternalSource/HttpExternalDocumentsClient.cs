using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerSync.Application.Abstractions.ExternalSource;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.ExternalSource;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LedgerSync.Infrastructure.ExternalSource;

/// <summary>
/// HTTP client of the external documents API. Retries, timeouts and circuit breaker
/// are provided by the resilience pipeline configured in DI.
/// </summary>
internal sealed class HttpExternalDocumentsClient(
    HttpClient httpClient,
    IOptions<ExternalApiOptions> options) : IExternalDocumentsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<IReadOnlyList<ExternalDocumentPayload>> FetchDocumentsAsync(CancellationToken cancellationToken)
    {
        JsonDocument json;
        try
        {
            using var response = await httpClient.GetAsync(options.Value.DocumentsPath, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new ExternalSourceUnavailableException(
                    $"External source returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new ExternalSourceInvalidResponseException("External source returned malformed JSON.", ex);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw new ExternalSourceUnavailableException($"External source is unavailable: {ex.Message}", ex);
        }

        using (json)
        {
            return ParseDocuments(json.RootElement);
        }
    }

    /// <summary>
    /// Parses items one by one: a single broken item is reported individually
    /// and does not invalidate the rest of the batch.
    /// </summary>
    private static List<ExternalDocumentPayload> ParseDocuments(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
            throw new ExternalSourceInvalidResponseException(
                $"Expected a JSON array of documents, got {root.ValueKind}.");

        var documents = new List<ExternalDocumentPayload>(root.GetArrayLength());
        foreach (var item in root.EnumerateArray())
        {
            try
            {
                documents.Add(item.Deserialize<ExternalDocumentPayload>(JsonOptions)
                              ?? ExternalDocumentPayload.Unreadable(null, "Item is null."));
            }
            catch (JsonException ex)
            {
                // Only the JSON path is reported: the raw exception text exposes internal type names.
                var field = string.IsNullOrEmpty(ex.Path) ? "item" : $"field '{ex.Path.TrimStart('$', '.')}'";
                documents.Add(ExternalDocumentPayload.Unreadable(TryReadExternalId(item), $"Document cannot be read: {field} has an invalid value."));
            }
        }

        return documents;
    }

    private static string? TryReadExternalId(JsonElement item) =>
        item.ValueKind == JsonValueKind.Object
        && item.TryGetProperty("externalId", out var id)
        && id.ValueKind == JsonValueKind.String
            ? id.GetString()
            : null;

    // Caller cancellation is propagated as is; everything else on the transport level means "unavailable".
    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException
        || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
