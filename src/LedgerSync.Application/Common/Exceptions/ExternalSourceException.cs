namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Base error of the external documents source. Aborts the sync run.
/// </summary>
public abstract class ExternalSourceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
