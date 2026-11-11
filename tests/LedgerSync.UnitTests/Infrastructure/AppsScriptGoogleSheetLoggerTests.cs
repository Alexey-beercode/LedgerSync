using System.Net;
using System.Text;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Infrastructure.GoogleSheets;
using LedgerSync.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace LedgerSync.UnitTests.Infrastructure;

/// <summary>
/// Real Google integration client, tested against a stubbed HTTP transport.
/// </summary>
public sealed class AppsScriptGoogleSheetLoggerTests
{
    private static readonly SyncLogDto Row = new(
        Guid.Parse("8f5d1cc9-0c7f-4c42-8f84-8ef86167fd63"),
        new DateTimeOffset(2026, 6, 25, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 6, 25, 10, 0, 5, TimeSpan.Zero),
        "Success", 10, 3, 2, 5, null);

    [Fact]
    public async Task Send_ScriptConfirms_SendsPayloadInAssignmentFormat()
    {
        var handler = Respond(HttpStatusCode.OK, """{ "ok": true }""");

        await CreateLogger(handler).SendSyncLogAsync(Row, CancellationToken.None);

        Assert.Contains("\"runId\":\"8f5d1cc9-0c7f-4c42-8f84-8ef86167fd63\"", handler.LastRequestBody);
        Assert.Contains("\"documentsReceived\":10", handler.LastRequestBody);
        Assert.Contains("\"startedAt\":\"2026-06-25T10:00:00Z\"", handler.LastRequestBody);
        Assert.Contains("\"status\":\"Success\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task Send_ScriptReportsError_Throws()
    {
        var handler = Respond(HttpStatusCode.OK, """{ "ok": false, "error": "sheet is protected" }""");

        var error = await Assert.ThrowsAsync<GoogleSheetDeliveryException>(() =>
            CreateLogger(handler).SendSyncLogAsync(Row, CancellationToken.None));
        Assert.Contains("sheet is protected", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, "<html>Apps Script error page</html>")]
    public async Task Send_BadResponse_Throws(HttpStatusCode status, string body) =>
        await Assert.ThrowsAsync<GoogleSheetDeliveryException>(() =>
            CreateLogger(Respond(status, body)).SendSyncLogAsync(Row, CancellationToken.None));

    [Fact]
    public async Task Send_NetworkFailure_Throws()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<GoogleSheetDeliveryException>(() =>
            CreateLogger(handler).SendSyncLogAsync(Row, CancellationToken.None));
    }

    private static StubHttpMessageHandler Respond(HttpStatusCode status, string body) =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

    private static AppsScriptGoogleSheetLogger CreateLogger(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(new GoogleSheetsOptions
        {
            Mode = GoogleSheetsMode.AppsScript,
            AppsScriptUrl = "https://script.example.test/exec"
        }));
}
