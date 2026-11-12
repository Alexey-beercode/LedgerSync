namespace LedgerSync.Application.Common.Exceptions;

/// <summary>
/// Requested order does not exist in the internal system.
/// </summary>
public sealed class OrderNotFoundException(string orderNumber)
    : Exception($"Order '{orderNumber}' was not found.")
{
    public string OrderNumber { get; } = orderNumber;
}
