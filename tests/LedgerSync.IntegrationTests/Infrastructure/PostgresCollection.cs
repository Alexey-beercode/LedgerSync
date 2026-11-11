namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// All integration tests share one PostgreSQL container and run sequentially.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
