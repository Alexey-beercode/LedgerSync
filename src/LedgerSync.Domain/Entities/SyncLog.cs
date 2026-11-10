using LedgerSync.Domain.Common;
using LedgerSync.Domain.Enums;
using LedgerSync.Domain.Exceptions;

namespace LedgerSync.Domain.Entities;

/// <summary>
/// History record of a single synchronization run. <see cref="Entity.Id"/> is used as RunId.
/// </summary>
public sealed class SyncLog : Entity
{
    private readonly List<SyncLogError> _errors = [];

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public SyncStatus Status { get; private set; }

    public int DocumentsReceived { get; private set; }

    public int DocumentsCreated { get; private set; }

    public int DocumentsUpdated { get; private set; }

    public int DocumentsSkipped { get; private set; }

    /// <summary>Fatal error that aborted the run, if any.</summary>
    public string? ErrorMessage { get; private set; }

    public GoogleSheetStatus GoogleSheetStatus { get; private set; }

    /// <summary>Last Google Sheets delivery error, if any.</summary>
    public string? GoogleSheetError { get; private set; }

    public DateTimeOffset? GoogleSheetSentAt { get; private set; }

    /// <summary>Per-document problems (invalid data, unknown order) collected during the run.</summary>
    public IReadOnlyCollection<SyncLogError> Errors => _errors.AsReadOnly();

    public bool IsFinished => Status != SyncStatus.Running;

    // Required by EF Core.
    private SyncLog() { }

    public static SyncLog Start(DateTimeOffset utcNow) => new()
    {
        StartedAt = utcNow,
        Status = SyncStatus.Running,
        GoogleSheetStatus = GoogleSheetStatus.Pending
    };

    public void SetReceived(int count)
    {
        EnsureRunning();
        DocumentsReceived = count;
    }

    public void RecordCreated()
    {
        EnsureRunning();
        DocumentsCreated++;
    }

    public void RecordUpdated()
    {
        EnsureRunning();
        DocumentsUpdated++;
    }

    public void RecordSkipped()
    {
        EnsureRunning();
        DocumentsSkipped++;
    }

    /// <summary>
    /// Registers a rejected document. It is counted as skipped and makes the run a partial success.
    /// </summary>
    public void RecordRejected(string? externalId, string reason)
    {
        EnsureRunning();
        DocumentsSkipped++;
        _errors.Add(SyncLogError.Create(Id, externalId, reason));
    }

    /// <summary>
    /// Completes the run. The status depends on whether any document was rejected.
    /// </summary>
    public void Complete(DateTimeOffset utcNow)
    {
        EnsureRunning();
        Status = _errors.Count == 0 ? SyncStatus.Success : SyncStatus.PartialSuccess;
        FinishedAt = utcNow;
    }

    /// <summary>
    /// Marks the run as failed with a fatal error.
    /// </summary>
    public void Fail(string errorMessage, DateTimeOffset utcNow)
    {
        EnsureRunning();
        Status = SyncStatus.Failed;
        ErrorMessage = errorMessage;
        FinishedAt = utcNow;
    }

    public void MarkGoogleSheetSent(DateTimeOffset utcNow)
    {
        GoogleSheetStatus = GoogleSheetStatus.Sent;
        GoogleSheetError = null;
        GoogleSheetSentAt = utcNow;
    }

    public void MarkGoogleSheetFailed(string error)
    {
        GoogleSheetStatus = GoogleSheetStatus.Failed;
        GoogleSheetError = error;
    }

    private void EnsureRunning()
    {
        if (IsFinished)
            throw new DomainException($"Sync run {Id} is already finished.");
    }
}
