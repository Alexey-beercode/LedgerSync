using LedgerSync.Application.Abstractions.Concurrency;

namespace LedgerSync.UnitTests.Fakes;

/// <summary>
/// Process-local replacement of the PostgreSQL advisory lock.
/// </summary>
internal sealed class InMemorySyncRunLock : ISyncRunLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IAsyncDisposable?>(_semaphore.Wait(0) ? new Releaser(_semaphore) : null);

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
