namespace LedgerSync.Domain.Enums;

/// <summary>
/// Processing status of a document in the external system.
/// Typical flow: Created → Sent → Signed, with Rejected / Cancelled as terminal branches.
/// </summary>
public enum DocumentStatus
{
    Created = 1,
    Sent = 2,
    Signed = 3,
    Rejected = 4,
    Cancelled = 5
}
