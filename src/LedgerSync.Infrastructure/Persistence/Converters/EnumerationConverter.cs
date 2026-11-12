using LedgerSync.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LedgerSync.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores an <see cref="Enumeration{TSelf}"/> as its name, so database values stay readable.
/// </summary>
public sealed class EnumerationConverter<T>() : ValueConverter<T, string>(
    value => value.Name,
    name => Enumeration<T>.FromName(name))
    where T : Enumeration<T>;
