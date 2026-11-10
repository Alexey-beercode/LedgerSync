namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Base error of the external documents source. Aborts the sync run.
/// </summary>
public abstract class ExternalSourceException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// External source is unreachable, timed out or returned a non-success status code.
/// </summary>
public sealed class ExternalSourceUnavailableException(string message, Exception? innerException = null)
    : ExternalSourceException(message, innerException);

/// <summary>
/// External source responded, but the body is not a valid documents payload.
/// </summary>
public sealed class ExternalSourceInvalidResponseException(string message, Exception? innerException = null)
    : ExternalSourceException(message, innerException);
