namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Requested sync run does not exist (or no runs were recorded yet when the latest one is requested).
/// </summary>
public sealed class SyncRunNotFoundException(Guid? runId = null)
    : Exception(runId is null
        ? "No synchronization runs have been recorded yet."
        : $"Sync run '{runId}' was not found.")
{
    public Guid? RunId { get; } = runId;
}
