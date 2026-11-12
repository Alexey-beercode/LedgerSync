using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using LedgerSync.Domain.Exceptions;

namespace LedgerSync.UnitTests.Domain;

public sealed class ExternalDocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Order Order = Order.Create("ORD-001", "Alpha", Now);

    private static ExternalDocument Create(decimal amount = 100m, DateTimeOffset? updatedAt = null) =>
        ExternalDocument.Create("EXT-1", Order, DocumentType.Invoice, "INV-1", amount, "kzt",
            DocumentStatus.Created, updatedAt ?? Now, Now);

    [Fact]
    public void Create_NormalizesCurrencyAndTimestampToUtc()
    {
        var document = Create(updatedAt: new DateTimeOffset(2026, 7, 1, 17, 0, 0, TimeSpan.FromHours(5)));

        Assert.Equal("KZT", document.Currency);
        Assert.Equal(TimeSpan.Zero, document.ExternalUpdatedAt.Offset);
        Assert.Equal(Now, document.ExternalUpdatedAt);
        Assert.Equal(Order.Id, document.OrderId);
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsWithFieldName()
    {
        var error = Assert.Throws<InvalidEntityDataException>(() => Create(amount: -1m));

        Assert.Equal(nameof(ExternalDocument.Amount), error.FieldName);
    }

    [Fact]
    public void HasChanges_SameValuesInOtherOffset_IsFalse()
    {
        var document = Create();

        Assert.False(document.HasChanges(DocumentStatus.Created, 100m, "kzt", Now.ToOffset(TimeSpan.FromHours(5))));
        Assert.True(document.HasChanges(DocumentStatus.Signed, 100m, "KZT", Now));
        Assert.True(document.HasChanges(DocumentStatus.Created, 101m, "KZT", Now));
        Assert.True(document.HasChanges(DocumentStatus.Created, 100m, "USD", Now));
        Assert.True(document.HasChanges(DocumentStatus.Created, 100m, "KZT", Now.AddSeconds(1)));
    }

    [Fact]
    public void ApplyExternalChanges_UpdatesAmountAndCurrencyTogether()
    {
        var document = Create();

        document.ApplyExternalChanges(DocumentStatus.Sent, 250m, "usd", Now.AddDays(1), Now.AddDays(1));

        Assert.Equal((250m, "USD"), (document.Amount, document.Currency));
    }

    [Fact]
    public void HasSameIdentity_DetectsOrderOrTypeChange()
    {
        var document = Create();

        Assert.True(document.HasSameIdentity("ORD-001", DocumentType.Invoice));
        Assert.False(document.HasSameIdentity("ORD-002", DocumentType.Invoice));
        Assert.False(document.HasSameIdentity("ORD-001", DocumentType.ActOfWork));
    }

    [Fact]
    public void IsNewerThan_DetectsOutOfOrderUpdate()
    {
        var document = Create(updatedAt: Now);

        Assert.True(document.IsNewerThan(Now.AddMinutes(-1)));
        Assert.False(document.IsNewerThan(Now));
    }
}
