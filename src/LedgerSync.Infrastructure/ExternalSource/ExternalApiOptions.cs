using System.ComponentModel.DataAnnotations;

namespace LedgerSync.Infrastructure.ExternalSource;

/// <summary>
/// External documents API settings. Section: <c>ExternalApi</c>.
/// </summary>
public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    [Required, Url]
    public string BaseUrl { get; init; } = null!;

    [Required]
    public string DocumentsPath { get; init; } = "/mock/external/documents";

    /// <summary>Timeout of a single HTTP attempt.</summary>
    [Range(1, 120)]
    public int AttemptTimeoutSeconds { get; init; } = 10;

    /// <summary>Retries on transient failures (5xx, 408, 429, network errors, timeouts).</summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Upper bound of the response size, protects memory from a runaway source.</summary>
    [Range(1, 100)]
    public int MaxResponseSizeMb { get; init; } = 10;
}
