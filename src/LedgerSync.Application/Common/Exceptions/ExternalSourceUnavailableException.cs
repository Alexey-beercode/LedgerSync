namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// External source is unreachable, timed out or returned a non-success status code.
/// </summary>
public sealed class ExternalSourceUnavailableException(string message, Exception? innerException = null)
    : ExternalSourceException(message, innerException);
