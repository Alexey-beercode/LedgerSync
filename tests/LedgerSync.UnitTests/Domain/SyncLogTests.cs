using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using LedgerSync.Domain.Exceptions;

namespace LedgerSync.UnitTests.Domain;

public sealed class SyncLogTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Complete_WithoutRejections_IsSuccess()
    {
        var log = SyncLog.Start(Now);
        log.SetReceived(2);
        log.RecordCreated();
        log.RecordSkipped();

        log.Complete(Now.AddSeconds(1));

        Assert.Equal(SyncStatus.Success, log.Status);
        Assert.Equal(Now.AddSeconds(1), log.FinishedAt);
    }

    [Fact]
    public void Complete_WithRejection_IsPartialSuccess()
    {
        var log = SyncLog.Start(Now);
        log.SetReceived(1);
        log.RecordRejected("EXT-1", "bad");

        log.Complete(Now);

        Assert.Equal(SyncStatus.PartialSuccess, log.Status);
        Assert.Equal(1, log.DocumentsSkipped);
    }

    [Fact]
    public void Complete_CountersDoNotAddUp_Throws()
    {
        var log = SyncLog.Start(Now);
        log.SetReceived(3);
        log.RecordCreated();

        Assert.Throws<InvalidEntityDataException>(() => log.Complete(Now));
    }

    [Fact]
    public void Fail_ResetsCountersAndErrors()
    {
        var log = SyncLog.Start(Now);
        log.SetReceived(2);
        log.RecordCreated();
        log.RecordRejected("EXT-2", "bad");

        log.Fail("boom", Now);

        Assert.Equal(SyncStatus.Failed, log.Status);
        Assert.Equal(2, log.DocumentsReceived);
        Assert.Equal(0, log.DocumentsCreated + log.DocumentsUpdated + log.DocumentsSkipped);
        Assert.Empty(log.Errors);
    }

    [Fact]
    public void FinishedRun_CannotBeChanged()
    {
        var log = SyncLog.Start(Now);
        log.Complete(Now);

        Assert.Throws<SyncRunAlreadyFinishedException>(() => log.RecordCreated());
        Assert.Throws<SyncRunAlreadyFinishedException>(() => log.Fail("late", Now));
    }

    [Fact]
    public void Fail_LongMessage_IsTruncatedToColumnSize()
    {
        var log = SyncLog.Start(Now);

        log.Fail(new string('x', 5000), Now);

        Assert.Equal(2000, log.ErrorMessage!.Length);
    }
}
