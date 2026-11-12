using LedgerSync.Domain.Common;
using LedgerSync.Domain.Enumerations;
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

    public SyncStatus Status { get; private set; } = null!;

    public int DocumentsReceived { get; private set; }

    public int DocumentsCreated { get; private set; }

    public int DocumentsUpdated { get; private set; }

    public int DocumentsSkipped { get; private set; }

    /// <summary>Fatal error that aborted the run, if any.</summary>
    public string? ErrorMessage { get; private set; }

    public GoogleSheetStatus GoogleSheetStatus { get; private set; } = null!;

    /// <summary>Last Google Sheets delivery error, if any.</summary>
    public string? GoogleSheetError { get; private set; }

    /// <summary>Per-document problems (invalid data, unknown order) collected during the run.</summary>
    public IReadOnlyCollection<SyncLogError> Errors => _errors.AsReadOnly();

    public bool IsFinished => Status != SyncStatus.Running;

    // Required by EF Core.
    private SyncLog() { }

    public static SyncLog Start(DateTimeOffset utcNow) => new()
    {
        StartedAt = Guard.Utc(utcNow),
        Status = SyncStatus.Running,
        GoogleSheetStatus = GoogleSheetStatus.Pending
    };

    public void SetReceived(int count)
    {
        EnsureRunning();
        DocumentsReceived = count >= 0
            ? count
            : throw new InvalidEntityDataException(nameof(SyncLog), nameof(DocumentsReceived), "value cannot be negative.");
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
    /// Completes the run. Status is Success, or PartialSuccess if any document was rejected.
    /// </summary>
    public void Complete(DateTimeOffset utcNow)
    {
        EnsureRunning();

        // Every received document must be accounted for exactly once.
        var processed = DocumentsCreated + DocumentsUpdated + DocumentsSkipped;
        if (processed != DocumentsReceived)
            throw new InvalidEntityDataException(nameof(SyncLog), nameof(DocumentsReceived),
                $"counters mismatch: received {DocumentsReceived}, processed {processed}.");

        Status = _errors.Count == 0 ? SyncStatus.Success : SyncStatus.PartialSuccess;
        FinishedAt = Guard.Utc(utcNow);
    }

    /// <summary>
    /// Marks the run as failed. Counters are reset because nothing from this run was persisted.
    /// </summary>
    public void Fail(string errorMessage, DateTimeOffset utcNow)
    {
        EnsureRunning();
        Status = SyncStatus.Failed;
        ErrorMessage = Truncate(errorMessage, FieldLimits.ErrorMessage);
        DocumentsCreated = 0;
        DocumentsUpdated = 0;
        DocumentsSkipped = 0;
        _errors.Clear();
        FinishedAt = Guard.Utc(utcNow);
    }

    public void MarkGoogleSheetSent()
    {
        GoogleSheetStatus = GoogleSheetStatus.Sent;
        GoogleSheetError = null;
    }

    public void MarkGoogleSheetFailed(string error)
    {
        GoogleSheetStatus = GoogleSheetStatus.Failed;
        GoogleSheetError = Truncate(error, FieldLimits.ErrorMessage);
    }

    private void EnsureRunning()
    {
        if (IsFinished)
            throw new SyncRunAlreadyFinishedException(Id, Status.Name);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
