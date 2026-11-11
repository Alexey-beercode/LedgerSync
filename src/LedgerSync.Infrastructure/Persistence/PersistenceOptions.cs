using System.ComponentModel.DataAnnotations;

namespace LedgerSync.Infrastructure.Persistence;

/// <summary>
/// Database settings. Section: <c>Persistence</c>; connection string: <c>ConnectionStrings:LedgerSync</c>.
/// </summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    public const string ConnectionStringName = "LedgerSync";

    /// <summary>Apply EF Core migrations on startup (convenient for local/Docker runs).</summary>
    public bool ApplyMigrationsOnStartup { get; init; }

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}
