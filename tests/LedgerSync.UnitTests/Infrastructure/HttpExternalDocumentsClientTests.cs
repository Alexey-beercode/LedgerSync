using System.Net;
using System.Text;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Infrastructure.ExternalSource;
using LedgerSync.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace LedgerSync.UnitTests.Infrastructure;

/// <summary>
/// Parsing and error mapping of the external documents client.
/// </summary>
public sealed class HttpExternalDocumentsClientTests
{
    [Fact]
    public async Task Fetch_OneUnreadableItem_ReturnsItAsUnreadableAndKeepsOthers()
    {
        const string body = """
            [
              { "externalId": "EXT-1", "orderNumber": "ORD-001", "amount": "150000.50" },
              { "externalId": "EXT-2", "orderNumber": "ORD-001", "amount": "not-a-number" }
            ]
            """;

        var documents = await CreateClient(HttpStatusCode.OK, body).FetchDocumentsAsync(CancellationToken.None);

        Assert.Equal(2, documents.Count);
        Assert.Equal(150000.50m, documents[0].Amount);
        Assert.Null(documents[0].ReadError);
        Assert.Equal("EXT-2", documents[1].ExternalId);
        Assert.Contains("'amount'", documents[1].ReadError);
        Assert.DoesNotContain("LedgerSync", documents[1].ReadError);
    }

    [Theory]
    [InlineData("""{ "documents": [] }""")]
    [InlineData("""[ { "externalId": """)]
    public async Task Fetch_NotAJsonArray_ThrowsInvalidResponse(string body) =>
        await Assert.ThrowsAsync<ExternalSourceInvalidResponseException>(() =>
            CreateClient(HttpStatusCode.OK, body).FetchDocumentsAsync(CancellationToken.None));

    [Fact]
    public async Task Fetch_ServerError_ThrowsUnavailable()
    {
        var error = await Assert.ThrowsAsync<ExternalSourceUnavailableException>(() =>
            CreateClient(HttpStatusCode.ServiceUnavailable, "{}").FetchDocumentsAsync(CancellationToken.None));

        Assert.Contains("503", error.Message);
    }

    [Fact]
    public async Task Fetch_NetworkFailure_ThrowsUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<ExternalSourceUnavailableException>(() =>
            CreateClient(handler).FetchDocumentsAsync(CancellationToken.None));
    }

    private static HttpExternalDocumentsClient CreateClient(HttpStatusCode status, string body) =>
        CreateClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") }));

    private static HttpExternalDocumentsClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://external.example.test") },
            Options.Create(new ExternalApiOptions { BaseUrl = "https://external.example.test" }));
}
