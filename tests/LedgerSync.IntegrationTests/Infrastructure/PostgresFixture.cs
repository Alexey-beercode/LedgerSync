using Npgsql;
using Testcontainers.PostgreSql;

namespace LedgerSync.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one real PostgreSQL container for all integration tests (requires Docker)
/// and hosts the API on top of it.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ledgersync")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    private LedgerSyncApiFactory? _api;
    private LedgerSyncApiFactory? _apiWithGoogleDown;

    /// <summary>API host with a working mock Google Sheet.</summary>
    public LedgerSyncApiFactory Api => _api ??= new LedgerSyncApiFactory(ConnectionString);

    /// <summary>API host where Google Sheets always fails.</summary>
    public LedgerSyncApiFactory ApiWithGoogleDown =>
        _apiWithGoogleDown ??= new LedgerSyncApiFactory(ConnectionString, googleSheetsDown: true);

    /// <summary>Removes synced data; seeded orders stay.</summary>
    public async Task ResetDatabaseAsync()
    {
        // Starting the host applies migrations, so the schema exists before truncating.
        using var _ = Api.CreateClient();

        await ExecuteAsync("TRUNCATE sync_log_errors, sync_logs, external_documents RESTART IDENTITY CASCADE");
    }

    public async Task<long> CountAsync(string sql) => (long)(await ExecuteAsync(sql))!;

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync()
    {
        if (_api is not null)
            await _api.DisposeAsync();
        if (_apiWithGoogleDown is not null)
            await _apiWithGoogleDown.DisposeAsync();

        await _container.DisposeAsync();
    }

    private async Task<object?> ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
