using LedgerSync.Application.Abstractions.Concurrency;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LedgerSync.Infrastructure.Concurrency;

/// <summary>
/// Cluster-wide sync lock based on a PostgreSQL session-level advisory lock.
/// The lock lives as long as its dedicated connection: if the process crashes,
/// PostgreSQL releases it automatically, so a run can never stay locked forever.
/// </summary>
internal sealed class PostgresAdvisorySyncRunLock(
    NpgsqlDataSource dataSource,
    ILogger<PostgresAdvisorySyncRunLock> logger) : ISyncRunLock
{
    // Arbitrary application-wide key for the document sync lock.
    private const long LockKey = 0x4C_65_64_67_65_72_01;

    public async Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", LockKey);

            if (await command.ExecuteScalarAsync(cancellationToken) is true)
                return new AdvisoryLockHandle(connection, LockKey, logger);

            await connection.DisposeAsync();
            return null;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
