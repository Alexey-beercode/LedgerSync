using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Enumerations;

/// <summary>
/// Overall result of a single synchronization run.
/// </summary>
public sealed class SyncStatus : Enumeration<SyncStatus>
{
    /// <summary>Run has started but not finished yet.</summary>
    public static readonly SyncStatus Running = new(0, nameof(Running));

    /// <summary>All received documents were processed without errors.</summary>
    public static readonly SyncStatus Success = new(1, nameof(Success));

    /// <summary>Run finished, but some documents were rejected (invalid data, unknown order).</summary>
    public static readonly SyncStatus PartialSuccess = new(2, nameof(PartialSuccess));

    /// <summary>Run aborted: source unavailable, malformed response or database error.</summary>
    public static readonly SyncStatus Failed = new(3, nameof(Failed));

    private SyncStatus(int id, string name) : base(id, name) { }
}
