namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// External source responded, but the body is not a valid documents payload.
/// </summary>
public sealed class ExternalSourceInvalidResponseException(string message, Exception? innerException = null)
    : ExternalSourceException(message, innerException);
