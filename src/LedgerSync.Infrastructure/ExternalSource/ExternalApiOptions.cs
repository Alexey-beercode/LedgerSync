using System.ComponentModel.DataAnnotations;

namespace LedgerSync.Infrastructure.ExternalSource;

/// <summary>
/// External documents API settings. Section: <c>ExternalApi</c>.
/// </summary>
public sealed class ExternalApiOptions : IValidatableObject
{
    public const string SectionName = "ExternalApi";

    /// <summary>Absolute http(s) address of the external system.</summary>
    [Required]
    public string BaseUrl { get; init; } = null!;

    /// <summary>Relative path of the documents endpoint, e.g. <c>/documents</c>.</summary>
    [Required]
    public string DocumentsPath { get; init; } = "/mock/external/documents";

    /// <summary>Retries on transient failures (5xx, 408, 429, network errors, timeouts).</summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            yield return new ValidationResult("BaseUrl must be an absolute http or https URL.", [nameof(BaseUrl)]);

        if (string.IsNullOrWhiteSpace(DocumentsPath)
            || !DocumentsPath.StartsWith('/')
            || !Uri.IsWellFormedUriString(DocumentsPath, UriKind.Relative))
            yield return new ValidationResult("DocumentsPath must be a relative path starting with '/'.", [nameof(DocumentsPath)]);
    }
}
