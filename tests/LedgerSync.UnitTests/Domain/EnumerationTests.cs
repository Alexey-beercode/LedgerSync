using LedgerSync.Domain.Enumerations;
using LedgerSync.Domain.Exceptions;

namespace LedgerSync.UnitTests.Domain;

public sealed class EnumerationTests
{
    [Fact]
    public void List_ContainsAllDeclaredValuesOrderedById() =>
        Assert.Equal(
            [DocumentStatus.Created, DocumentStatus.Sent, DocumentStatus.Signed, DocumentStatus.Rejected, DocumentStatus.Cancelled],
            DocumentStatus.List);

    [Theory]
    [InlineData("Signed")]
    [InlineData("signed")]
    [InlineData(" SIGNED ")]
    public void FromName_IgnoresCaseAndSpaces(string name) =>
        Assert.Same(DocumentStatus.Signed, DocumentStatus.FromName(name));

    [Fact]
    public void FromName_UnknownValue_ThrowsSpecificException()
    {
        var error = Assert.Throws<UnknownEnumerationValueException>(() => DocumentType.FromName("Receipt"));

        Assert.Equal(nameof(DocumentType), error.EnumerationName);
        Assert.Equal("Receipt", error.Value);
    }

    [Fact]
    public void TryFromName_Null_ReturnsFalse() =>
        Assert.False(SyncStatus.TryFromName(null, out _));

    [Fact]
    public void Equality_IsBasedOnValue()
    {
        Assert.True(DocumentStatus.FromName("signed") == DocumentStatus.Signed);
        Assert.True(DocumentStatus.Sent != DocumentStatus.Signed);
    }
}
