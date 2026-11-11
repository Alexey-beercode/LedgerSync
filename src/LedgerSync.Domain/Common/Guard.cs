using LedgerSync.Domain.Exceptions;

namespace LedgerSync.Domain.Common;

/// <summary>
/// Small helpers for checking entity invariants.
/// </summary>
internal static class Guard
{
    public static string NotEmpty(string? value, string entity, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidEntityDataException(entity, field, "value is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new InvalidEntityDataException(entity, field, $"length must not exceed {maxLength}.");

        return trimmed;
    }

    public static decimal NotNegative(decimal value, string entity, string field) =>
        value >= 0 ? value : throw new InvalidEntityDataException(entity, field, "value cannot be negative.");

    public static T NotNull<T>(T? value, string entity, string field) where T : class =>
        value ?? throw new InvalidEntityDataException(entity, field, "value is required.");

    /// <summary>
    /// Normalizes a timestamp to UTC (PostgreSQL <c>timestamptz</c> accepts only zero offset).
    /// </summary>
    public static DateTimeOffset Utc(DateTimeOffset value) => value.ToUniversalTime();
}
