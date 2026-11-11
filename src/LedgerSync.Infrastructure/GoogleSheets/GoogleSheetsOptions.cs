using System.ComponentModel.DataAnnotations;

namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Google Sheets journal settings. Section: <c>GoogleSheets</c>.
/// </summary>
public sealed class GoogleSheetsOptions : IValidatableObject
{
    public const string SectionName = "GoogleSheets";

    /// <summary>Required on purpose: production must choose the integration explicitly, never fall back to the mock.</summary>
    [Required]
    public GoogleSheetsMode? Mode { get; init; }

    /// <summary>CSV file used by the mock logger (<see cref="GoogleSheetsMode.File"/>).</summary>
    public string FilePath { get; init; } = "google-sheets-mock/sync-log.csv";

    /// <summary>
    /// Apps Script Web App URL (<see cref="GoogleSheetsMode.AppsScript"/>).
    /// Treated as a secret: set via user-secrets or environment variable, never commit it.
    /// </summary>
    public string? AppsScriptUrl { get; init; }

    [Range(1, 120)]
    public int AttemptTimeoutSeconds { get; init; } = 15;

    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 2;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Mode == GoogleSheetsMode.AppsScript && !Uri.TryCreate(AppsScriptUrl, UriKind.Absolute, out _))
            yield return new ValidationResult("AppsScriptUrl must be an absolute URL in AppsScript mode.", [nameof(AppsScriptUrl)]);

        if (Mode == GoogleSheetsMode.File && string.IsNullOrWhiteSpace(FilePath))
            yield return new ValidationResult("FilePath is required in File mode.", [nameof(FilePath)]);
    }
}
