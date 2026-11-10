namespace LedgerSync.Domain.Enums;

/// <summary>
/// Overall result of a single synchronization run.
/// </summary>
public enum SyncStatus
{
    /// <summary>Run has started but not finished yet.</summary>
    Running = 0,

    /// <summary>All received documents were processed without errors.</summary>
    Success = 1,

    /// <summary>Run finished, but some documents were rejected (invalid data, unknown order).</summary>
    PartialSuccess = 2,

    /// <summary>Run aborted, e.g. the external source was unavailable.</summary>
    Failed = 3
}
