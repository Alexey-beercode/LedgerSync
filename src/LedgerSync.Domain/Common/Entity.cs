namespace LedgerSync.Domain.Common;

/// <summary>
/// Base class for all entities identified by a GUID.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
