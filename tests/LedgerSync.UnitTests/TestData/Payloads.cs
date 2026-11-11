using LedgerSync.Application.Contracts.ExternalSource;

namespace LedgerSync.UnitTests.TestData;

/// <summary>
/// Builders of external payloads with sensible defaults.
/// </summary>
internal static class Payloads
{
    public static readonly DateTimeOffset BaseTime = new(2026, 6, 20, 10, 0, 0, TimeSpan.Zero);

    public static ExternalDocumentPayload Document(
        string externalId = "EXT-INV-001",
        string orderNumber = "ORD-001",
        string documentType = "Invoice",
        string status = "Created",
        decimal amount = 150_000m,
        DateTimeOffset? updatedAt = null) =>
        new(externalId, orderNumber, documentType, $"DOC-{externalId}", amount, "KZT", status, updatedAt ?? BaseTime);
}
