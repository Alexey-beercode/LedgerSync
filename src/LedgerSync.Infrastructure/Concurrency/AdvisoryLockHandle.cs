using Microsoft.Extensions.Logging;
using Npgsql;

namespace LedgerSync.Infrastructure.Concurrency;

/// <summary>
/// Held advisory lock. Disposing releases the lock and closes its dedicated connection.
/// </summary>
internal sealed class AdvisoryLockHandle(NpgsqlConnection connection, long lockKey, ILogger logger) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
            command.Parameters.AddWithValue("key", lockKey);
            await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            // Closing the connection below still releases the lock on the server side.
            logger.LogWarning(ex, "Failed to release sync advisory lock explicitly");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
