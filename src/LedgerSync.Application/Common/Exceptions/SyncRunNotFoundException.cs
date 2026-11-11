namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// No sync run exists yet (e.g. resend to Google requested before the first sync).
/// </summary>
public sealed class SyncRunNotFoundException()
    : Exception("No synchronization runs have been recorded yet.");
