namespace LedgerSync.Domain.Exceptions;

/// <summary>
/// Thrown when an entity is created or changed with values that break its invariants
/// (empty required field, negative amount, etc.).
/// </summary>
public sealed class InvalidEntityDataException(string entityName, string fieldName, string reason)
    : Exception($"{entityName}.{fieldName}: {reason}")
{
    public string EntityName { get; } = entityName;

    public string FieldName { get; } = fieldName;
}
