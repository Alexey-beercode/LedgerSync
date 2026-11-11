using System.ComponentModel.DataAnnotations;

namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Google Sheets journal settings. Section: <c>GoogleSheets</c>.
/// </summary>
public sealed class GoogleSheetsOptions : IValidatableObject
{
    public const string SectionName = "GoogleSheets";

    public GoogleSheetsMode Mode { get; init; } = GoogleSheetsMode.File;

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

/// <summary>
/// Which <c>IGoogleSheetLogger</c> implementation is used.
/// </summary>
public enum GoogleSheetsMode
{
    /// <summary>Mock: rows are written to a local CSV file shaped like the sheet.</summary>
    File,

    /// <summary>Real integration: HTTP POST to a Google Apps Script Web App.</summary>
    AppsScript,

    /// <summary>Demo mode that always fails, to show error handling and manual resend.</summary>
    Unavailable
}
