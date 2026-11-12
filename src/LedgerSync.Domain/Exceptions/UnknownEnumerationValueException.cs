namespace LedgerSync.Domain.Exceptions;

/// <summary>
/// Thrown when a value cannot be mapped to a known enumeration member (e.g. an unknown document status).
/// </summary>
public sealed class UnknownEnumerationValueException(string enumerationName, string? value)
    : Exception($"'{value}' is not a valid {enumerationName}.")
{
    public string EnumerationName { get; } = enumerationName;

    public string? Value { get; } = value;
}
