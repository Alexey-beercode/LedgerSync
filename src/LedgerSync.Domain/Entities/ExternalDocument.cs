using LedgerSync.Domain.Common;
using LedgerSync.Domain.Enums;
using LedgerSync.Domain.Exceptions;

namespace LedgerSync.Domain.Entities;

/// <summary>
/// Financial document received from the external system.
/// Uniquely identified by <see cref="ExternalId"/>.
/// </summary>
public sealed class ExternalDocument : Entity
{
    /// <summary>Document identifier in the external system (deduplication key).</summary>
    public string ExternalId { get; private set; } = null!;

    public Guid OrderId { get; private set; }

    /// <summary>Order number as received from the external system.</summary>
    public string OrderNumber { get; private set; } = null!;

    public DocumentType DocumentType { get; private set; }

    public string DocumentNumber { get; private set; } = null!;

    public decimal Amount { get; private set; }

    /// <summary>ISO 4217 currency code (e.g. "KZT").</summary>
    public string Currency { get; private set; } = null!;

    public DocumentStatus Status { get; private set; }

    /// <summary>Last modification time in the external system.</summary>
    public DateTimeOffset ExternalUpdatedAt { get; private set; }

    /// <summary>When the record was created in the internal system.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the record was last updated in the internal system.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order Order { get; private set; } = null!;

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
        if (string.IsNullOrWhiteSpace(externalId))
            throw new DomainException("External id is required.");
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new DomainException("Document number is required.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");
        if (amount < 0)
            throw new DomainException("Amount cannot be negative.");

        return new ExternalDocument
        {
            ExternalId = externalId.Trim(),
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Order = order,
            DocumentType = documentType,
            DocumentNumber = documentNumber.Trim(),
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            Status = status,
            ExternalUpdatedAt = externalUpdatedAt,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>
    /// Returns true when tracked fields (status, amount, external timestamp) differ from the given values.
    /// </summary>
    public bool HasChanges(DocumentStatus status, decimal amount, DateTimeOffset externalUpdatedAt) =>
        Status != status || Amount != amount || ExternalUpdatedAt != externalUpdatedAt;

    /// <summary>
    /// Returns true when the stored version is newer than the incoming one (out-of-order delivery).
    /// </summary>
    public bool IsNewerThan(DateTimeOffset externalUpdatedAt) => ExternalUpdatedAt > externalUpdatedAt;

    /// <summary>
    /// Applies changes coming from the external system.
    /// </summary>
    public void ApplyExternalChanges(
        DocumentStatus status,
        decimal amount,
        DateTimeOffset externalUpdatedAt,
        DateTimeOffset utcNow)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative.");

        Status = status;
        Amount = amount;
        ExternalUpdatedAt = externalUpdatedAt;
        UpdatedAt = utcNow;
    }
}
