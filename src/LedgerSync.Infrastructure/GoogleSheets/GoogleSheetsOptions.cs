using System.ComponentModel.DataAnnotations;

namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Google Sheets journal settings. Section: <c>GoogleSheets</c>.
/// </summary>
public sealed class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    /// <summary>CSV file of the mock sheet, with the same columns as the real one.</summary>
    [Required]
    public string FilePath { get; init; } = "google-sheets-mock/sync-log.csv";
}
