using System.Net.Http.Json;
using LedgerSync.Application.Abstractions.Concurrency;
using LedgerSync.Application.Abstractions.Persistence;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using LedgerSync.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSync.IntegrationTests;

/// <summary>
/// Guarantees that must hold at the database level, independent of application code.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ReliabilityTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UniqueIndex_RejectsSecondDocumentWithSameExternalId()
    {
        var services = postgres.Api().Services;
        var order = await GetOrderAsync(services, "ORD-001");

        await AddDocumentAsync(services, order);
        var conflict = await Assert.ThrowsAsync<PersistenceConflictException>(() => AddDocumentAsync(services, order));

        Assert.Contains("ix_external_documents_external_id", conflict.Message);
        Assert.Equal(1, await postgres.CountAsync("SELECT count(*) FROM external_documents"));
    }

    [Fact]
    public async Task AdvisoryLock_IsExclusiveAcrossConnectionsAndReleasedOnDispose()
    {
        var syncLock = postgres.Api().Services.GetRequiredService<ISyncRunLock>();

        await using (var first = await syncLock.TryAcquireAsync(CancellationToken.None))
        {
            Assert.NotNull(first);
            Assert.Null(await syncLock.TryAcquireAsync(CancellationToken.None));
        }

        await using var again = await syncLock.TryAcquireAsync(CancellationToken.None);
        Assert.NotNull(again);
    }

    [Fact]
    public async Task GoogleSheetsDown_DocumentsStaySavedAndRunCanBeResent()
    {
        var client = postgres.Api("Unavailable").CreateClient();
        (await client.PutAsync("/mock/external/scenario/Baseline", null)).EnsureSuccessStatusCode();

        var response = await client.PostAsync("/api/sync/documents", null);
        var result = (await response.Content.ReadFromJsonAsync<SyncResultDto>())!;

        Assert.Equal("Success", result.Status);
        Assert.Equal("Failed", result.GoogleSheetStatus);
        Assert.Equal(4, await postgres.CountAsync("SELECT count(*) FROM external_documents"));

        var resend = await client.PostAsync("/api/sync/logs/latest/send-to-google", null);
        var resendResult = (await resend.Content.ReadFromJsonAsync<GoogleSheetResendResultDto>())!;
        Assert.Equal(result.RunId, resendResult.RunId);
        Assert.Equal("Failed", resendResult.GoogleSheetStatus);
        Assert.NotNull(resendResult.GoogleSheetError);
    }

    private static async Task<Order> GetOrderAsync(IServiceProvider services, string orderNumber)
    {
        await using var scope = services.CreateAsyncScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        return (await orders.GetByNumberAsync(orderNumber, CancellationToken.None))!;
    }

    // Each insert uses its own scope, as two independent processes would.
    private static async Task AddDocumentAsync(IServiceProvider services, Order order)
    {
        await using var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IExternalDocumentRepository>().Add(ExternalDocument.Create(
            "EXT-DUP-1", order, DocumentType.Invoice, "INV-DUP", 10m, "KZT", DocumentStatus.Created, Now, Now));
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }
}
