namespace LedgerSync.Domain.Enums;

/// <summary>
/// Delivery state of a sync log row to Google Sheets.
/// </summary>
public enum GoogleSheetStatus
{
    /// <summary>Not sent yet.</summary>
    Pending = 0,

    /// <summary>Row was successfully appended to the sheet.</summary>
    Sent = 1,

    /// <summary>Delivery failed; can be retried manually.</summary>
    Failed = 2
}
