using LedgerSync.Application.Contracts.SyncLogs;
using LedgerSync.Infrastructure.GoogleSheets;
using Microsoft.Extensions.Options;

namespace LedgerSync.UnitTests.Infrastructure;

/// <summary>
/// Mock Google sheet (CSV): one row per run, upsert by RunId, safe escaping.
/// </summary>
public sealed class FileGoogleSheetLoggerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"ledgersync-test-{Guid.NewGuid():N}.csv");
    private readonly FileGoogleSheetLogger _logger;

    public FileGoogleSheetLoggerTests() =>
        _logger = new FileGoogleSheetLogger(Options.Create(new GoogleSheetsOptions
        {
            FilePath = _path
        }));

    [Fact]
    public async Task Send_SameRunTwice_KeepsOneUpdatedRow()
    {
        var runId = Guid.NewGuid();

        await _logger.SendSyncLogAsync(Row(runId, "Failed", "first"), CancellationToken.None);
        await _logger.SendSyncLogAsync(Row(runId, "Success", null), CancellationToken.None);
        await _logger.SendSyncLogAsync(Row(Guid.NewGuid(), "Success", null), CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(_path);
        Assert.Equal(3, lines.Length); // header + 2 runs
        Assert.StartsWith("RunId,StartedAt,FinishedAt,Status", lines[0]);
        Assert.Contains(",Success,", Assert.Single(lines, l => l.StartsWith(runId.ToString())));
    }

    [Fact]
    public async Task Send_ErrorWithCommaQuotesAndNewline_StaysOneEscapedCell()
    {
        var runId = Guid.NewGuid();

        await _logger.SendSyncLogAsync(Row(runId, "Failed", "bad, \"quoted\"\nsecond line"), CancellationToken.None);

        var row = (await File.ReadAllLinesAsync(_path))[1];
        Assert.EndsWith("\"bad, \"\"quoted\"\" second line\"", row);
    }

    public void Dispose() => File.Delete(_path);

    private static SyncLogDto Row(Guid runId, string status, string? error) => new(
        runId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, status, 1, 1, 0, 0, error);
}
