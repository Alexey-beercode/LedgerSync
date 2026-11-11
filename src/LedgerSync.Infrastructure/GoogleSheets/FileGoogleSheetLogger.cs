using System.Globalization;
using System.Text;
using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;
using Microsoft.Extensions.Options;

namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Mock Google Sheets logger: keeps the sheet as a local CSV file with the same columns.
/// Rows are upserted by RunId, so resending the same run never creates a duplicate row.
/// </summary>
internal sealed class FileGoogleSheetLogger(IOptions<GoogleSheetsOptions> options) : IGoogleSheetLogger
{
    private const string Header =
        "RunId,StartedAt,FinishedAt,Status,DocumentsReceived,DocumentsCreated,DocumentsUpdated,DocumentsSkipped,ErrorMessage";

    // Serializes writers inside the process; the file is replaced atomically on each write.
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public async Task SendSyncLogAsync(SyncLogDto syncLog, CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(options.Value.FilePath);

        await FileLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var rows = File.Exists(path)
                ? (await File.ReadAllLinesAsync(path, cancellationToken)).Skip(1).ToList()
                : [];

            var runKey = syncLog.RunId + ",";
            var newRow = ToCsvRow(syncLog);
            var index = rows.FindIndex(r => r.StartsWith(runKey, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                rows[index] = newRow;
            else
                rows.Add(newRow);

            // Write to a temp file and swap, so a crash never leaves a half-written sheet.
            var tempPath = path + ".tmp";
            await File.WriteAllLinesAsync(tempPath, [Header, .. rows], Encoding.UTF8, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new GoogleSheetDeliveryException($"Cannot write mock sheet '{path}': {ex.Message}", ex);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private static string ToCsvRow(SyncLogDto log) => string.Join(',',
        log.RunId,
        log.StartedAt.ToString("O", CultureInfo.InvariantCulture),
        log.FinishedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
        log.Status,
        log.DocumentsReceived,
        log.DocumentsCreated,
        log.DocumentsUpdated,
        log.DocumentsSkipped,
        Escape(log.ErrorMessage));

    // Newlines are flattened to keep one run per line; quotes are escaped per RFC 4180.
    private static string Escape(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : "\"" + value.ReplaceLineEndings(" ").Replace("\"", "\"\"") + "\"";
}
