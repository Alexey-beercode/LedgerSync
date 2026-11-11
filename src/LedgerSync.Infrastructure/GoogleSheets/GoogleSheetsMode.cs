namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Which <c>IGoogleSheetLogger</c> implementation is used.
/// </summary>
public enum GoogleSheetsMode
{
    /// <summary>Mock: rows are written to a local CSV file shaped like the sheet.</summary>
    File,

    /// <summary>Real integration: HTTP POST to a Google Apps Script Web App.</summary>
    AppsScript
}
