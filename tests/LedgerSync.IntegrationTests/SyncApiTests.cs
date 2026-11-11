using System.Net;
using System.Net.Http.Json;
using LedgerSync.Application.Abstractions.Concurrency;
using LedgerSync.Application.Contracts.Orders;
using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSync.IntegrationTests;

/// <summary>
/// End-to-end tests over HTTP: API → use case → PostgreSQL, with the mock external system and mock sheet.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SyncApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly HttpClient _client = postgres.Api().CreateClient();

    public async Task InitializeAsync()
    {
        await postgres.ResetDatabaseAsync();
        await SetScenarioAsync("Baseline");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Sync_RunTwice_StoresEachDocumentOnce()
    {
        var first = await SyncAsync();
        var second = await SyncAsync();

        Assert.Equal(4, first.DocumentsCreated);
        Assert.Equal(0, second.DocumentsCreated);
        Assert.Equal(4, second.DocumentsSkipped);
        Assert.Equal(4, await postgres.CountAsync("SELECT count(*) FROM external_documents"));
        Assert.Equal(2, await postgres.CountAsync("SELECT count(*) FROM sync_logs WHERE status = 'Success'"));
    }

    [Fact]
    public async Task Sync_ChangedSource_UpdatesAndCreates()
    {
        await SyncAsync();
        await SetScenarioAsync("Updated");

        var result = await SyncAsync();

        Assert.Equal((1, 2, 2), (result.DocumentsCreated, result.DocumentsUpdated, result.DocumentsSkipped));

        var order = await _client.GetFromJsonAsync<OrderDocumentsDto>("/api/orders/ORD-002/documents");
        var invoice = Assert.Single(order!.Documents);
        Assert.Equal("Signed", invoice.Status);
        Assert.Equal(85000.00m, invoice.Amount);
    }

    [Fact]
    public async Task Sync_MixedBatch_SavesValidDocumentsAndRecordsRejections()
    {
        await SetScenarioAsync("Mixed");

        var result = await SyncAsync();

        Assert.Equal("PartialSuccess", result.Status);
        Assert.Equal(2, await postgres.CountAsync("SELECT count(*) FROM external_documents"));

        var latest = (await _client.GetFromJsonAsync<List<SyncLogHistoryItemDto>>("/api/sync/logs?take=1"))!.Single();
        Assert.Equal(
            ["EXT-BAD-001", "EXT-BAD-002", "EXT-INV-404"],
            latest.Errors.Select(e => e.ExternalId).Order());
    }

    [Theory]
    [InlineData("Unavailable")]
    [InlineData("Malformed")]
    public async Task Sync_SourceBroken_ReturnsFailedRunAndPersistsIt(string scenario)
    {
        await SetScenarioAsync(scenario);

        var result = await SyncAsync();

        Assert.Equal("Failed", result.Status);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        Assert.Equal(1, await postgres.CountAsync("SELECT count(*) FROM sync_logs WHERE status = 'Failed' AND error_message IS NOT NULL"));
    }

    [Fact]
    public async Task Sync_WhileAnotherRunHoldsLock_Returns409()
    {
        var syncLock = postgres.Api().Services.GetRequiredService<ISyncRunLock>();
        await using var held = await syncLock.TryAcquireAsync(CancellationToken.None);
        Assert.NotNull(held);

        var response = await _client.PostAsync("/api/sync/documents", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, await postgres.CountAsync("SELECT count(*) FROM sync_logs"));
    }

    [Fact]
    public async Task Sync_SuccessfulRun_IsWrittenToMockSheetOnce()
    {
        var result = await SyncAsync();
        await _client.PostAsync("/api/sync/logs/latest/send-to-google", null);

        var rows = await File.ReadAllLinesAsync(postgres.Api().SheetPath);
        Assert.Single(rows, r => r.StartsWith(result.RunId.ToString()));
    }

    [Fact]
    public async Task Orders_UnknownOrder_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/orders/ORD-404/documents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("ORD-404", problem!.Detail);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task Logs_InvalidTake_Returns400()
    {
        var response = await _client.GetAsync("/api/sync/logs?take=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_Ready_ChecksDatabase()
    {
        Assert.Equal("Healthy", await _client.GetStringAsync("/health/ready"));
    }

    private async Task<SyncResultDto> SyncAsync()
    {
        var response = await _client.PostAsync("/api/sync/documents", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SyncResultDto>())!;
    }

    private async Task SetScenarioAsync(string scenario) =>
        (await _client.PutAsync($"/mock/external/scenario/{scenario}", null)).EnsureSuccessStatusCode();
}
