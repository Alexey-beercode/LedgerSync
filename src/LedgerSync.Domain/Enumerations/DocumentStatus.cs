using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Enumerations;

/// <summary>
/// Processing status of a document in the external system.
/// Typical flow: Created → Sent → Signed, with Rejected / Cancelled as terminal branches.
/// The external system is the source of truth, so transitions are not restricted here.
/// </summary>
public sealed class DocumentStatus : Enumeration<DocumentStatus>
{
    public static readonly DocumentStatus Created = new(1, nameof(Created), isTerminal: false);
    public static readonly DocumentStatus Sent = new(2, nameof(Sent), isTerminal: false);
    public static readonly DocumentStatus Signed = new(3, nameof(Signed), isTerminal: true);
    public static readonly DocumentStatus Rejected = new(4, nameof(Rejected), isTerminal: true);
    public static readonly DocumentStatus Cancelled = new(5, nameof(Cancelled), isTerminal: true);

    private DocumentStatus(int id, string name, bool isTerminal) : base(id, name)
    {
        IsTerminal = isTerminal;
    }

    /// <summary>True when the document lifecycle is normally finished in this status.</summary>
    public bool IsTerminal { get; }
}
