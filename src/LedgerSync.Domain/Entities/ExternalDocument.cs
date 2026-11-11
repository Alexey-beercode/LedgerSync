using LedgerSync.Domain.Common;
using LedgerSync.Domain.Enumerations;

namespace LedgerSync.Domain.Entities;

/// <summary>
/// Financial document received from the external system.
/// Uniquely identified by <see cref="ExternalId"/> (deduplication key).
/// </summary>
public sealed class ExternalDocument : Entity
{
    private const string EntityName = nameof(ExternalDocument);

    /// <summary>Document identifier in the external system.</summary>
    public string ExternalId { get; private set; } = null!;

    /// <summary>Owning order. Only the key is kept, so saving a document never touches the order row.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Order number the document belongs to.</summary>
    public string OrderNumber { get; private set; } = null!;

    public DocumentType DocumentType { get; private set; } = null!;

    public string DocumentNumber { get; private set; } = null!;

    public decimal Amount { get; private set; }

    /// <summary>ISO 4217 currency code (e.g. "KZT").</summary>
    public string Currency { get; private set; } = null!;

    public DocumentStatus Status { get; private set; } = null!;

    /// <summary>Last modification time in the external system (UTC).</summary>
    public DateTimeOffset ExternalUpdatedAt { get; private set; }

    /// <summary>When the record was created in the internal system (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the record was last updated in the internal system (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core.
    private ExternalDocument() { }

    public static ExternalDocument Create(
        string externalId,
        Order order,
        DocumentType documentType,
        string documentNumber,
        decimal amount,
        string currency,
        DocumentStatus status,
        DateTimeOffset externalUpdatedAt,
        DateTimeOffset utcNow)
    {
        Guard.NotNull(order, EntityName, nameof(Order));

        return new ExternalDocument
        {
            ExternalId = Guard.NotEmpty(externalId, EntityName, nameof(ExternalId), FieldLimits.ExternalId),
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            DocumentType = Guard.NotNull(documentType, EntityName, nameof(DocumentType)),
            DocumentNumber = Guard.NotEmpty(documentNumber, EntityName, nameof(DocumentNumber), FieldLimits.DocumentNumber),
            Amount = Guard.NotNegative(amount, EntityName, nameof(Amount)),
            Currency = Guard.NotEmpty(currency, EntityName, nameof(Currency), FieldLimits.CurrencyCode).ToUpperInvariant(),
            Status = Guard.NotNull(status, EntityName, nameof(Status)),
            ExternalUpdatedAt = Guard.Utc(externalUpdatedAt),
            CreatedAt = Guard.Utc(utcNow),
            UpdatedAt = Guard.Utc(utcNow)
        };
    }

    /// <summary>
    /// True when tracked fields (status, amount, external timestamp) differ from the incoming values.
    /// </summary>
    public bool HasChanges(DocumentStatus status, decimal amount, DateTimeOffset externalUpdatedAt) =>
        Status != status || Amount != amount || ExternalUpdatedAt != Guard.Utc(externalUpdatedAt);

    /// <summary>
    /// True when the stored version is newer than the incoming one (out-of-order delivery).
    /// Such updates must be ignored, otherwise an old status could overwrite a newer one.
    /// </summary>
    public bool IsNewerThan(DateTimeOffset externalUpdatedAt) => ExternalUpdatedAt > Guard.Utc(externalUpdatedAt);

    /// <summary>
    /// Applies changes coming from the external system.
    /// </summary>
    public void ApplyExternalChanges(
        DocumentStatus status,
        decimal amount,
        DateTimeOffset externalUpdatedAt,
        DateTimeOffset utcNow)
    {
        Status = Guard.NotNull(status, EntityName, nameof(Status));
        Amount = Guard.NotNegative(amount, EntityName, nameof(Amount));
        ExternalUpdatedAt = Guard.Utc(externalUpdatedAt);
        UpdatedAt = Guard.Utc(utcNow);
    }
}
