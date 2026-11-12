namespace LedgerSync.Domain.Exceptions;

/// <summary>
/// Thrown when a finished sync run is modified (counters or status changed after completion).
/// </summary>
public sealed class SyncRunAlreadyFinishedException(Guid runId, string status)
    : Exception($"Sync run {runId} is already finished with status '{status}'.")
{
    public Guid RunId { get; } = runId;
}
