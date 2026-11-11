using System.Net.Http.Json;
using System.Text.Json;
using LedgerSync.Application.Abstractions.GoogleSheets;
using LedgerSync.Application.Common.Exceptions;
using LedgerSync.Application.Contracts.SyncLogs;
using Microsoft.Extensions.Options;

namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Real Google Sheets integration through an Apps Script Web App (see <c>google-apps-script/Code.gs</c>).
/// The script upserts by runId, so HTTP retries and manual resends are idempotent.
/// </summary>
internal sealed class AppsScriptGoogleSheetLogger(
    HttpClient httpClient,
    IOptions<GoogleSheetsOptions> options) : IGoogleSheetLogger
{
    public async Task SendSyncLogAsync(SyncLogDto syncLog, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(options.Value.AppsScriptUrl, syncLog, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new GoogleSheetDeliveryException($"Apps Script returned HTTP {(int)response.StatusCode}.");

            // Apps Script answers 200 even when the script fails, so the body is checked explicitly.
            var result = await response.Content.ReadFromJsonAsync<AppsScriptResponse>(cancellationToken);
            if (result is not { Ok: true })
                throw new GoogleSheetDeliveryException($"Apps Script rejected the row: {result?.Error ?? "empty response"}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
                                   || ex is Polly.Timeout.TimeoutRejectedException
                                   || ex is Polly.CircuitBreaker.BrokenCircuitException)
        {
            throw new GoogleSheetDeliveryException($"Google Sheets is unavailable: {ex.Message}", ex);
        }
    }

    private sealed record AppsScriptResponse(bool Ok, string? Error);
}
