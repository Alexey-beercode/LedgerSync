namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Save was rejected by the database because of a unique constraint or an optimistic concurrency conflict.
/// Nothing from the failed save is persisted.
/// </summary>
public sealed class PersistenceConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
