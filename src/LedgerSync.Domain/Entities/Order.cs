using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Entities;

/// <summary>
/// Internal client order that external documents are attached to.
/// </summary>
public sealed class Order : Entity
{
    private readonly List<ExternalDocument> _documents = [];

    /// <summary>Business order number, unique (e.g. "ORD-001").</summary>
    public string OrderNumber { get; private set; } = null!;

    public string ClientName { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<ExternalDocument> Documents => _documents.AsReadOnly();

    // Required by EF Core.
    private Order() { }

    public static Order Create(string orderNumber, string clientName, DateTimeOffset utcNow) => new()
    {
        OrderNumber = Guard.NotEmpty(orderNumber, nameof(Order), nameof(OrderNumber), FieldLimits.OrderNumber),
        ClientName = Guard.NotEmpty(clientName, nameof(Order), nameof(ClientName), FieldLimits.ClientName),
        CreatedAt = Guard.Utc(utcNow)
    };
}
