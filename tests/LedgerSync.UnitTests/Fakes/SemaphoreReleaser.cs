namespace LedgerSync.UnitTests.Fakes;

/// <summary>
/// Releases the held semaphore slot on dispose.
/// </summary>
internal sealed class SemaphoreReleaser(SemaphoreSlim semaphore) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        semaphore.Release();
        return ValueTask.CompletedTask;
    }
}
