namespace LedgerSync.Domain.Common;

/// <summary>
/// Maximum lengths of text fields. Shared by domain checks, validation and database schema.
/// </summary>
public static class FieldLimits
{
    public const int ExternalId = 100;
    public const int OrderNumber = 50;
    public const int ClientName = 200;
    public const int DocumentNumber = 100;
    public const int CurrencyCode = 3;
    public const int EnumerationName = 32;
    public const int ErrorMessage = 2000;
    public const int ErrorReason = 1000;
}
