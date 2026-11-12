namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Sync log row could not be delivered to Google Sheets. Never rolls back saved documents.
/// </summary>
public sealed class GoogleSheetDeliveryException(string message, Exception? innerException = null)
    : Exception(message, innerException);
