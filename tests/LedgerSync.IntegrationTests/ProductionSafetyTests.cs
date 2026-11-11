using System.Net;
using System.Net.Http.Json;
using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Infrastructure.Persistence;
using LedgerSync.IntegrationTests.Infrastructure;

namespace LedgerSync.IntegrationTests;

/// <summary>
/// Demo features stay out of production, and recovery endpoints work against the real database.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ProductionSafetyTests(PostgresFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MockExternalApi_Disabled_IsNotReachable()
    {
        await using var api = new LedgerSyncApiFactory(postgres.ConnectionString, mockExternalApiEnabled: false);
        var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/mock/external/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync("/mock/external/scenario/Unavailable", null)).StatusCode);
    }

    [Fact]
    public async Task Production_WithoutExplicitConfiguration_RefusesToStart()
    {
        await using var api = new UnconfiguredApiFactory(postgres.ConnectionString);

        var error = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("BaseUrl", error.ToString());
    }

    [Fact]
    public async Task Api_Timestamps_UseUtcWithZSuffix()
    {
        var client = postgres.Api.CreateClient();
        (await client.PutAsync("/mock/external/scenario/Baseline", null)).EnsureSuccessStatusCode();
        await client.PostAsync("/api/sync/documents", null);

        var json = await client.GetStringAsync("/api/orders/ORD-001/documents");

        Assert.Contains("\"externalUpdatedAt\":\"2026-06-20T10:00:00Z\"", json);
    }

    [Fact]
    public async Task DemoSeeder_RunTwice_InsertsOrdersOnce()
    {
        await postgres.Api.Services.SeedDemoDataAsync();
        await postgres.Api.Services.SeedDemoDataAsync();

        Assert.Equal(3, await postgres.CountAsync("SELECT count(*) FROM orders"));
    }

    [Fact]
    public async Task ResendByRunId_OlderUndeliveredRun_IsDelivered()
    {
        var googleDown = postgres.ApiWithGoogleDown.CreateClient();
        (await googleDown.PutAsync("/mock/external/scenario/Baseline", null)).EnsureSuccessStatusCode();
        var undelivered = await (await googleDown.PostAsync("/api/sync/documents", null))
            .Content.ReadFromJsonAsync<SyncResultDto>();
        Assert.Equal("Failed", undelivered!.GoogleSheetStatus);

        var client = postgres.Api.CreateClient();
        await client.PostAsync("/api/sync/documents", null);

        var response = await client.PostAsync($"/api/sync/logs/{undelivered.RunId}/send-to-google", null);
        var result = await response.Content.ReadFromJsonAsync<GoogleSheetResendResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((undelivered.RunId, "Sent"), (result!.RunId, result.GoogleSheetStatus));
        Assert.Contains(await File.ReadAllLinesAsync(postgres.Api.SheetPath), row => row.StartsWith(undelivered.RunId.ToString()));
    }

    [Fact]
    public async Task ResendByRunId_UnknownRun_Returns404()
    {
        var response = await postgres.Api.CreateClient().PostAsync($"/api/sync/logs/{Guid.NewGuid()}/send-to-google", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
