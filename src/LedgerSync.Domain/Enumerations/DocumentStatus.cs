using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Enumerations;

/// <summary>
/// Processing status of a document in the external system.
/// Typical flow: Created → Sent → Signed, with Rejected / Cancelled as terminal branches.
/// The external system is the source of truth, so transitions are not restricted here.
/// </summary>
public sealed class DocumentStatus : Enumeration<DocumentStatus>
{
    public static readonly DocumentStatus Created = new(1, nameof(Created));
    public static readonly DocumentStatus Sent = new(2, nameof(Sent));
    public static readonly DocumentStatus Signed = new(3, nameof(Signed));
    public static readonly DocumentStatus Rejected = new(4, nameof(Rejected));
    public static readonly DocumentStatus Cancelled = new(5, nameof(Cancelled));

    private DocumentStatus(int id, string name) : base(id, name) { }
}
