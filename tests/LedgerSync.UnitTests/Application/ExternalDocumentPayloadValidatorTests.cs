using LedgerSync.Application.Contracts.ExternalSource;
using LedgerSync.Application.Features.Sync.SyncDocuments;
using Microsoft.Extensions.Time.Testing;
using static LedgerSync.UnitTests.TestData.Payloads;

namespace LedgerSync.UnitTests.Application;

public sealed class ExternalDocumentPayloadValidatorTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly ExternalDocumentPayloadValidator _validator = new(Time);

    [Fact]
    public void Validate_CompleteDocument_IsValid() =>
        Assert.True(_validator.Validate(Document()).IsValid);

    [Theory]
    [InlineData("invoice")]
    [InlineData("ACTOFWORK")]
    public void Validate_TypeInAnyCase_IsValid(string documentType) =>
        Assert.True(_validator.Validate(Document(documentType: documentType)).IsValid);

    public static TheoryData<string, ExternalDocumentPayload> InvalidDocuments() => new()
    {
        { "ExternalId", Document() with { ExternalId = " " } },
        { "OrderNumber", Document() with { OrderNumber = null } },
        { "DocumentType", Document(documentType: "Receipt") },
        { "Status", Document(status: "Paid") },
        { "Amount", Document(amount: -0.01m) },
        { "Amount", Document() with { Amount = null } },
        { "Amount", Document(amount: 1.001m) },
        { "Currency", Document() with { Currency = "TENGE" } },
        { "ExternalUpdatedAt", Document() with { ExternalUpdatedAt = null } },
        { "ExternalUpdatedAt", Document(updatedAt: Time.GetUtcNow().AddHours(1)) }
    };

    [Theory]
    [MemberData(nameof(InvalidDocuments))]
    public void Validate_InvalidField_IsReportedForThatField(string field, ExternalDocumentPayload payload)
    {
        var result = _validator.Validate(payload);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == field);
    }

    [Fact]
    public void Validate_UnreadableItem_ReportsOnlyReadError()
    {
        var result = _validator.Validate(ExternalDocumentPayload.Unreadable("EXT-1", "field 'amount' has an invalid value"));

        var error = Assert.Single(result.Errors);
        Assert.Contains("amount", error.ErrorMessage);
    }
}
