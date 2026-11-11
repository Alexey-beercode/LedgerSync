namespace LedgerSync.Infrastructure.GoogleSheets;

/// <summary>
/// Body returned by <c>Code.gs</c>: <c>{ "ok": true }</c> or <c>{ "ok": false, "error": "..." }</c>.
/// </summary>
internal sealed record AppsScriptResponse(bool Ok, string? Error);
