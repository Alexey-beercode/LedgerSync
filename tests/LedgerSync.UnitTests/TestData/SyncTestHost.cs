using LedgerSync.Application.Contracts.Sync;
using LedgerSync.Application.Sync;
using LedgerSync.Application.Validation;
using LedgerSync.Domain.Entities;
using LedgerSync.Infrastructure.Persistence;
using LedgerSync.Infrastructure.Persistence.Repositories;
using LedgerSync.UnitTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LedgerSync.UnitTests.TestData;

/// <summary>
/// Wires the real sync use case and repositories to an isolated in-memory database and test doubles.
/// Each call uses a fresh DbContext, like a separate HTTP request scope.
/// </summary>
internal sealed class SyncTestHost
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    public SyncTestHost()
    {
        // Applies HasData seed (ORD-001..ORD-003).
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
    public FakeExternalDocumentsClient ExternalSource { get; } = new();
    public RecordingGoogleSheetLogger GoogleSheets { get; } = new();
    public InMemorySyncRunLock SyncLock { get; } = new();
    public FailingSaveInterceptor SaveInterceptor { get; } = new();

    public LedgerSyncDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<LedgerSyncDbContext>()
            .UseInMemoryDatabase(_databaseName, _databaseRoot)
            .AddInterceptors(SaveInterceptor)
            .Options);

    public async Task<SyncResultDto> RunSyncAsync()
    {
        await using var db = CreateDbContext();
        return await CreateSyncService(db).RunAsync(CancellationToken.None);
    }

    public DocumentSyncService CreateSyncService(LedgerSyncDbContext db)
    {
        var unitOfWork = new UnitOfWork(db);
        return new DocumentSyncService(
            SyncLock,
            ExternalSource,
            new OrderRepository(db),
            new ExternalDocumentRepository(db),
            new SyncLogRepository(db),
            unitOfWork,
            new ExternalDocumentPayloadValidator(Time),
            CreateReporter(db, unitOfWork),
            Time,
            NullLogger<DocumentSyncService>.Instance);
    }

    public SyncLogService CreateSyncLogService(LedgerSyncDbContext db) =>
        new(new SyncLogRepository(db), new SyncLogsQueryValidator(), CreateReporter(db, new UnitOfWork(db)));

    public async Task<List<ExternalDocument>> GetDocumentsAsync()
    {
        await using var db = CreateDbContext();
        return await db.ExternalDocuments.AsNoTracking().OrderBy(d => d.ExternalId).ToListAsync();
    }

    public async Task<SyncLog> GetSyncLogAsync(Guid runId)
    {
        await using var db = CreateDbContext();
        return await db.SyncLogs.AsNoTracking().Include(s => s.Errors).SingleAsync(s => s.Id == runId);
    }

    private GoogleSheetReporter CreateReporter(LedgerSyncDbContext db, UnitOfWork unitOfWork) =>
        new(GoogleSheets, unitOfWork, Time, NullLogger<GoogleSheetReporter>.Instance);
}
