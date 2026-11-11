using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.ExternalSource;
using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using LedgerSync.UnitTests.TestData;
using static LedgerSync.UnitTests.TestData.Payloads;

namespace LedgerSync.UnitTests.Application;

/// <summary>
/// Sync command handler against real repositories (EF Core in-memory) with stubbed external systems.
/// Tests 1–7 cover the mandatory scenarios of the assignment, the rest cover reliability rules.
/// </summary>
public sealed class SyncDocumentsCommandHandlerTests
{
    private readonly SyncTestHost _host = new();

    // 1. Repeated sync with the same data does not create duplicates.
    [Fact]
    public async Task Handle_SameDataTwice_DoesNotCreateDuplicates()
    {
        _host.ExternalSource.Returns(Document("EXT-INV-001"), Document("EXT-ACT-001", documentType: "ActOfWork"));

        await _host.RunSyncAsync();
        var second = await _host.RunSyncAsync();

        Assert.Equal(2, (await _host.GetDocumentsAsync()).Count);
        Assert.Equal("Success", second.Status);
        Assert.Equal(0, second.DocumentsCreated);
        Assert.Equal(0, second.DocumentsUpdated);
        Assert.Equal(2, second.DocumentsSkipped);
    }

    // 2. A new external document is created.
    [Fact]
    public async Task Handle_NewDocument_IsCreatedWithAllFields()
    {
        _host.ExternalSource.Returns(Document("EXT-INV-001", amount: 150_000.50m, status: "Sent"));

        var result = await _host.RunSyncAsync();

        var document = Assert.Single(await _host.GetDocumentsAsync());
        Assert.Equal(1, result.DocumentsCreated);
        Assert.Equal("EXT-INV-001", document.ExternalId);
        Assert.Equal("ORD-001", document.OrderNumber);
        Assert.Equal(DocumentType.Invoice, document.DocumentType);
        Assert.Equal(DocumentStatus.Sent, document.Status);
        Assert.Equal(150_000.50m, document.Amount);
        Assert.Equal(BaseTime, document.ExternalUpdatedAt);
    }

    // 3. A status change updates the internal document.
    [Fact]
    public async Task Handle_StatusChanged_UpdatesDocument()
    {
        _host.ExternalSource.Returns(Document(status: "Created"));
        await _host.RunSyncAsync();

        _host.ExternalSource.Returns(Document(status: "Signed", updatedAt: BaseTime.AddDays(1)));
        var result = await _host.RunSyncAsync();

        var document = Assert.Single(await _host.GetDocumentsAsync());
        Assert.Equal(1, result.DocumentsUpdated);
        Assert.Equal(DocumentStatus.Signed, document.Status);
        Assert.Equal(BaseTime.AddDays(1), document.ExternalUpdatedAt);
        Assert.Equal(_host.Time.GetUtcNow(), document.UpdatedAt);
    }

    // 4. A document of a non-existent order is rejected and recorded in SyncLog errors.
    [Fact]
    public async Task Handle_UnknownOrder_RejectsDocumentAndKeepsOthers()
    {
        _host.ExternalSource.Returns(Document("EXT-INV-001"), Document("EXT-INV-404", orderNumber: "ORD-404"));

        var result = await _host.RunSyncAsync();

        Assert.Equal("PartialSuccess", result.Status);
        Assert.Equal(1, result.DocumentsCreated);
        Assert.Equal(1, result.DocumentsSkipped);
        Assert.DoesNotContain(await _host.GetDocumentsAsync(), d => d.ExternalId == "EXT-INV-404");

        var error = Assert.Single((await _host.GetSyncLogAsync(result.RunId)).Errors);
        Assert.Equal("EXT-INV-404", error.ExternalId);
        Assert.Contains("ORD-404", error.Reason);
    }

    // 5. An external source error is saved in SyncLog.
    [Theory]
    [MemberData(nameof(SourceErrors))]
    public async Task Handle_ExternalSourceFails_RecordsFailedRun(Exception sourceError)
    {
        _host.ExternalSource.Throws(sourceError);

        var result = await _host.RunSyncAsync();

        var log = await _host.GetSyncLogAsync(result.RunId);
        Assert.Equal(SyncStatus.Failed, log.Status);
        Assert.Equal(sourceError.Message, log.ErrorMessage);
        Assert.NotNull(log.FinishedAt);
        Assert.Empty(await _host.GetDocumentsAsync());
    }

    public static TheoryData<Exception> SourceErrors() => new()
    {
        new ExternalSourceUnavailableException("External source returned HTTP 503."),
        new ExternalSourceInvalidResponseException("External source returned malformed JSON.")
    };

    // 6. A successful run is sent to IGoogleSheetLogger.
    [Fact]
    public async Task Handle_Success_SendsRunToGoogleSheets()
    {
        _host.ExternalSource.Returns(Document("EXT-INV-001"), Document("EXT-INV-002", orderNumber: "ORD-002"));

        var result = await _host.RunSyncAsync();

        var row = Assert.Single(_host.GoogleSheets.SentRows);
        Assert.Equal(result.RunId, row.RunId);
        Assert.Equal("Success", row.Status);
        Assert.Equal(2, row.DocumentsReceived);
        Assert.Equal(2, row.DocumentsCreated);
        Assert.NotNull(row.FinishedAt);
        Assert.Equal("Sent", result.GoogleSheetStatus);
    }

    // 7. A Google Sheets error does not roll back saved documents.
    [Fact]
    public async Task Handle_GoogleSheetsUnavailable_KeepsDocumentsAndRecordsDeliveryFailure()
    {
        _host.GoogleSheets.IsUnavailable = true;
        _host.ExternalSource.Returns(Document("EXT-INV-001"));

        var result = await _host.RunSyncAsync();

        Assert.Single(await _host.GetDocumentsAsync());
        Assert.Equal("Success", result.Status);
        Assert.Equal("Failed", result.GoogleSheetStatus);

        var log = await _host.GetSyncLogAsync(result.RunId);
        Assert.Equal(GoogleSheetStatus.Failed, log.GoogleSheetStatus);
        Assert.Contains("down", log.GoogleSheetError);
    }

    [Fact]
    public async Task Handle_DatabaseSaveFails_RollsBackAllDocumentsAndRecordsFailedRun()
    {
        _host.SaveInterceptor.FailDocumentSaves = true;
        _host.ExternalSource.Returns(Document("EXT-INV-001"), Document("EXT-INV-002", orderNumber: "ORD-002"));

        var result = await _host.RunSyncAsync();

        Assert.Empty(await _host.GetDocumentsAsync());
        Assert.Equal("Failed", result.Status);
        Assert.Equal(2, result.DocumentsReceived);
        Assert.Equal(0, result.DocumentsCreated);
        Assert.Contains("no changes were applied", result.ErrorMessage);

        // The failure itself still reaches Google Sheets.
        Assert.Equal("Failed", Assert.Single(_host.GoogleSheets.SentRows).Status);
    }

    [Fact]
    public async Task Handle_OlderVersionOfKnownDocument_IsIgnored()
    {
        _host.ExternalSource.Returns(Document(status: "Signed", updatedAt: BaseTime));
        await _host.RunSyncAsync();

        _host.ExternalSource.Returns(Document(status: "Created", updatedAt: BaseTime.AddHours(-1)));
        var result = await _host.RunSyncAsync();

        Assert.Equal(1, result.DocumentsSkipped);
        Assert.Equal(DocumentStatus.Signed, Assert.Single(await _host.GetDocumentsAsync()).Status);
    }

    [Fact]
    public async Task Handle_CurrencyChanged_UpdatesAmountTogetherWithCurrency()
    {
        _host.ExternalSource.Returns(Document(amount: 150_000m, currency: "KZT"));
        await _host.RunSyncAsync();

        _host.ExternalSource.Returns(Document(amount: 300m, currency: "USD", updatedAt: BaseTime.AddDays(1)));
        var result = await _host.RunSyncAsync();

        var document = Assert.Single(await _host.GetDocumentsAsync());
        Assert.Equal(1, result.DocumentsUpdated);
        Assert.Equal((300m, "USD"), (document.Amount, document.Currency));
    }

    [Fact]
    public async Task Handle_KnownDocumentMovedToAnotherOrder_IsRejectedAndNotChanged()
    {
        _host.ExternalSource.Returns(Document(orderNumber: "ORD-001"));
        await _host.RunSyncAsync();

        _host.ExternalSource.Returns(Document(orderNumber: "ORD-002", status: "Sent", updatedAt: BaseTime.AddDays(1)));
        var result = await _host.RunSyncAsync();

        Assert.Equal("PartialSuccess", result.Status);
        var document = Assert.Single(await _host.GetDocumentsAsync());
        Assert.Equal(("ORD-001", DocumentStatus.Created), (document.OrderNumber, document.Status));
        Assert.Contains("cannot change its order", Assert.Single((await _host.GetSyncLogAsync(result.RunId)).Errors).Reason);
    }

    [Fact]
    public async Task Handle_SameDocumentTwiceInBatch_StoresLatestVersionOnce()
    {
        _host.ExternalSource.Returns(
            Document(status: "Created", updatedAt: BaseTime),
            Document(status: "Sent", updatedAt: BaseTime.AddHours(2)));

        var result = await _host.RunSyncAsync();

        Assert.Equal(DocumentStatus.Sent, Assert.Single(await _host.GetDocumentsAsync()).Status);
        Assert.Equal(1, result.DocumentsCreated);
        Assert.Equal(1, result.DocumentsSkipped);
        Assert.Equal("Success", result.Status);
    }

    [Fact]
    public async Task Handle_InvalidItems_AreRejectedWhileValidOnesAreSaved()
    {
        _host.ExternalSource.Returns(
            Document("EXT-INV-001"),
            Document("EXT-BAD-001", status: "Paid"),
            Document("EXT-BAD-002", amount: -1m),
            Document("EXT-BAD-003", updatedAt: _host.Time.GetUtcNow().AddDays(1)),
            ExternalDocumentPayload.Unreadable("EXT-BAD-004", "unreadable"));

        var result = await _host.RunSyncAsync();

        Assert.Equal("PartialSuccess", result.Status);
        Assert.Equal(5, result.DocumentsReceived);
        Assert.Equal(1, result.DocumentsCreated);
        Assert.Equal(4, result.DocumentsSkipped);
        Assert.Equal(4, (await _host.GetSyncLogAsync(result.RunId)).Errors.Count);
    }

    [Fact]
    public async Task Handle_AnotherRunInProgress_IsRejectedWithoutTouchingSource()
    {
        await using var heldLock = await _host.SyncLock.TryAcquireAsync(CancellationToken.None);

        await Assert.ThrowsAsync<SyncAlreadyRunningException>(_host.RunSyncAsync);
        Assert.Equal(0, _host.ExternalSource.CallCount);
    }

    [Fact]
    public async Task Handle_RunLeftRunningAfterCrash_IsMarkedAsFailed()
    {
        var crashed = SyncLog.Start(_host.Time.GetUtcNow().AddHours(-1));
        await using (var db = _host.CreateDbContext())
        {
            db.SyncLogs.Add(crashed);
            await db.SaveChangesAsync();
        }

        _host.ExternalSource.Returns(Document());
        await _host.RunSyncAsync();

        var recovered = await _host.GetSyncLogAsync(crashed.Id);
        Assert.Equal(SyncStatus.Failed, recovered.Status);
        Assert.Contains("interrupted", recovered.ErrorMessage);
    }

    [Fact]
    public async Task Handle_AnyOutcome_CountersAddUpToReceived()
    {
        _host.ExternalSource.Returns(Document("EXT-1"), Document("EXT-2", orderNumber: "ORD-404"), Document("EXT-1"));
        await _host.RunSyncAsync();
        _host.ExternalSource.Returns(Document("EXT-1", status: "Sent", updatedAt: BaseTime.AddDays(1)), Document("EXT-3"));

        var result = await _host.RunSyncAsync();

        Assert.Equal(result.DocumentsReceived, result.DocumentsCreated + result.DocumentsUpdated + result.DocumentsSkipped);
    }
}
