namespace LedgerSync.Application.Abstractions.Concurrency;

/// <summary>
/// Guarantees that only one sync run is executed at a time, across all application instances.
/// </summary>
public interface ISyncRunLock
{
    /// <summary>
    /// Tries to acquire the lock without waiting. Returns a handle that releases the lock on dispose,
    /// or null if another run currently holds it.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken);
}
