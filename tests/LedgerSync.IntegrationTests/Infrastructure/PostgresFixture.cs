using Npgsql;
using Testcontainers.PostgreSql;

namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one real PostgreSQL container for all integration tests (requires Docker)
/// and hosts the API on top of it with different Google Sheets modes.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ledgersync")
        .Build();

    private readonly Dictionary<string, LedgerSyncApiFactory> _factories = [];

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>API host with the given <c>GoogleSheets:Mode</c>, created once and reused.</summary>
    public LedgerSyncApiFactory Api(string googleSheetsMode = "File")
    {
        if (!_factories.TryGetValue(googleSheetsMode, out var factory))
        {
            factory = new LedgerSyncApiFactory(ConnectionString, googleSheetsMode);
            _factories[googleSheetsMode] = factory;
        }

        return factory;
    }

    /// <summary>Removes synced data; seeded orders stay.</summary>
    public async Task ResetDatabaseAsync()
    {
        // Starting the host applies migrations, so the schema exists before truncating.
        using var _ = Api().CreateClient();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "TRUNCATE sync_log_errors, sync_logs, external_documents RESTART IDENTITY CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<long> CountAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync()
    {
        foreach (var factory in _factories.Values)
            await factory.DisposeAsync();

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
