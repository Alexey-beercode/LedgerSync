using LedgerSync.Domain.Common;

namespace LedgerSync.Domain.Entities;

/// <summary>
/// Problem with a single document during a sync run (invalid payload, unknown order, etc.).
/// </summary>
public sealed class SyncLogError : Entity
{
    public Guid SyncLogId { get; private set; }

    /// <summary>External id of the document, if it could be read from the payload.</summary>
    public string? ExternalId { get; private set; }

    public string Reason { get; private set; } = null!;

    // Required by EF Core.
    private SyncLogError() { }

    internal static SyncLogError Create(Guid syncLogId, string? externalId, string reason) => new()
    {
        SyncLogId = syncLogId,
        ExternalId = externalId is { Length: > FieldLimits.ExternalId } ? externalId[..FieldLimits.ExternalId] : externalId,
        Reason = reason.Length > FieldLimits.ErrorReason ? reason[..FieldLimits.ErrorReason] : reason
    };
}
