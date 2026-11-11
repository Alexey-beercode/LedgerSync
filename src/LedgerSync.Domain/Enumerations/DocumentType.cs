using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Enumerations;

/// <summary>
/// Kind of financial document received from the external system.
/// </summary>
public sealed class DocumentType : Enumeration<DocumentType>
{
    /// <summary>Invoice issued to the client.</summary>
    public static readonly DocumentType Invoice = new(1, nameof(Invoice));

    /// <summary>Act of completed work.</summary>
    public static readonly DocumentType ActOfWork = new(2, nameof(ActOfWork));

    private DocumentType(int id, string name) : base(id, name) { }
}
