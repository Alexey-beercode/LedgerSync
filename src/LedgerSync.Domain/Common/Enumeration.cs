using System.Reflection;
using LedgerSync.Domain.Exceptions;

namespace LedgerSync.Domain.Common;

/// <summary>
/// Base class for type-safe enumerations ("smart enums").
/// Unlike a plain enum, a value can carry behavior and cannot hold an undefined number.
/// All values are discovered from <c>public static readonly</c> fields of <typeparamref name="TSelf"/>.
/// </summary>
public abstract class Enumeration<TSelf> : IEquatable<TSelf>
    where TSelf : Enumeration<TSelf>
{
    private static readonly Lazy<IReadOnlyList<TSelf>> AllValues = new(DiscoverValues);

    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Stable numeric identifier.</summary>
    public int Id { get; }

    /// <summary>Stable name, used for persistence and API contracts.</summary>
    public string Name { get; }

    public static IReadOnlyList<TSelf> List => AllValues.Value;

    /// <summary>Resolves a value by name (case-insensitive).</summary>
    /// <exception cref="UnknownEnumerationValueException">No value with this name.</exception>
    public static TSelf FromName(string name) =>
        TryFromName(name, out var value)
            ? value
            : throw new UnknownEnumerationValueException(typeof(TSelf).Name, name);

    /// <summary>Safe lookup by name for untrusted input.</summary>
    public static bool TryFromName(string? name, out TSelf value)
    {
        value = List.FirstOrDefault(v => string.Equals(v.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase))!;
        return value is not null;
    }

    public bool Equals(TSelf? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => obj is TSelf other && Equals(other);

    public override int GetHashCode() => Id;

    public override string ToString() => Name;

    public static bool operator ==(Enumeration<TSelf>? left, Enumeration<TSelf>? right) =>
        left is null ? right is null : left.Equals(right as TSelf);

    public static bool operator !=(Enumeration<TSelf>? left, Enumeration<TSelf>? right) => !(left == right);

    private static IReadOnlyList<TSelf> DiscoverValues() =>
        typeof(TSelf)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TSelf))
            .Select(f => (TSelf)f.GetValue(null)!)
            .OrderBy(v => v.Id)
            .ToList();
}
