namespace LedgerSync.Domain.Enums;

/// <summary>
/// Kind of financial document received from the external system.
/// </summary>
public enum DocumentType
{
    /// <summary>Invoice issued to the client.</summary>
    Invoice = 1,

    /// <summary>Act of completed work.</summary>
    ActOfWork = 2
}
