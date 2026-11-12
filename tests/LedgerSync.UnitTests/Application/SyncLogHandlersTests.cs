using FluentValidation;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Features.SyncLogs.GetSyncLogs;
using LedgerSync.Application.Features.SyncLogs.ResendSyncLog;
using LedgerSync.UnitTests.TestData;
using static LedgerSync.UnitTests.TestData.Payloads;

namespace LedgerSync.UnitTests.Application;

/// <summary>
/// GetSyncLogs query and ResendLatestSyncLog command.
/// </summary>
public sealed class SyncLogHandlersTests
{
    private readonly SyncTestHost _host = new();

    [Fact]
    public async Task ResendLatest_AfterGoogleOutage_DeliversSameRunOnce()
    {
        _host.GoogleSheets.IsUnavailable = true;
        _host.ExternalSource.Returns(Document());
        var run = await _host.RunSyncAsync();

        _host.GoogleSheets.IsUnavailable = false;
        await using var db = _host.CreateDbContext();
        var result = await _host.CreateResendHandler(db).HandleAsync(new ResendSyncLogCommand(), CancellationToken.None);

        Assert.Equal(run.RunId, result.RunId);
        Assert.Equal("Sent", result.GoogleSheetStatus);
        Assert.Equal(run.RunId, Assert.Single(_host.GoogleSheets.SentRows).RunId);
    }

    [Fact]
    public async Task ResendLatest_NoRunsYet_Throws()
    {
        await using var db = _host.CreateDbContext();

        await Assert.ThrowsAsync<SyncRunNotFoundException>(() =>
            _host.CreateResendHandler(db).HandleAsync(new ResendSyncLogCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task ResendByRunId_OlderUndeliveredRun_IsDeliveredEvenAfterNewerRuns()
    {
        _host.GoogleSheets.IsUnavailable = true;
        _host.ExternalSource.Returns(Document());
        var undelivered = await _host.RunSyncAsync();

        _host.GoogleSheets.IsUnavailable = false;
        _host.Time.Advance(TimeSpan.FromMinutes(1));
        await _host.RunSyncAsync();

        await using var db = _host.CreateDbContext();
        var result = await _host.CreateResendHandler(db)
            .HandleAsync(new ResendSyncLogCommand(undelivered.RunId), CancellationToken.None);

        Assert.Equal(undelivered.RunId, result.RunId);
        Assert.Equal("Sent", result.GoogleSheetStatus);
        Assert.Contains(_host.GoogleSheets.SentRows, row => row.RunId == undelivered.RunId);
    }

    [Fact]
    public async Task ResendByRunId_UnknownRun_Throws()
    {
        await using var db = _host.CreateDbContext();
        var runId = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<SyncRunNotFoundException>(() =>
            _host.CreateResendHandler(db).HandleAsync(new ResendSyncLogCommand(runId), CancellationToken.None));
        Assert.Equal(runId, error.RunId);
    }

    [Fact]
    public async Task GetSyncLogs_ReturnsNewestFirstWithErrors()
    {
        _host.ExternalSource.Returns(Document());
        var first = await _host.RunSyncAsync();
        _host.Time.Advance(TimeSpan.FromMinutes(1));
        _host.ExternalSource.Returns(Document("EXT-404", orderNumber: "ORD-404"));
        var second = await _host.RunSyncAsync();

        await using var db = _host.CreateDbContext();
        var history = await _host.CreateGetSyncLogsHandler(db).HandleAsync(new GetSyncLogsQuery(10), CancellationToken.None);

        Assert.Equal([second.RunId, first.RunId], history.Select(h => h.RunId));
        Assert.Single(history[0].Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetSyncLogs_TakeOutOfRange_ThrowsValidation(int take)
    {
        await using var db = _host.CreateDbContext();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _host.CreateGetSyncLogsHandler(db).HandleAsync(new GetSyncLogsQuery(take), CancellationToken.None));
    }
}
