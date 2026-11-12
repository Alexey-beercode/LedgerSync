namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Save was rejected by a database unique constraint (e.g. a second document with the same ExternalId).
/// Nothing from the failed save is persisted.
/// </summary>
public sealed class PersistenceConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
