using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Enumerations;

/// <summary>
/// Delivery state of a sync run row to Google Sheets.
/// </summary>
public sealed class GoogleSheetStatus : Enumeration<GoogleSheetStatus>
{
    /// <summary>Not sent yet.</summary>
    public static readonly GoogleSheetStatus Pending = new(0, nameof(Pending));

    /// <summary>Row was successfully written to the sheet.</summary>
    public static readonly GoogleSheetStatus Sent = new(1, nameof(Sent));

    /// <summary>Delivery failed; can be retried manually.</summary>
    public static readonly GoogleSheetStatus Failed = new(2, nameof(Failed));

    private GoogleSheetStatus(int id, string name) : base(id, name) { }
}
