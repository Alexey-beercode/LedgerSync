using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LedgerSync.UnitTests.Fakes;

/// <summary>
/// Simulates a database failure on the save that contains document changes.
/// </summary>
internal sealed class FailingSaveInterceptor : SaveChangesInterceptor
{
    public bool FailDocumentSaves { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var touchesDocuments = eventData.Context!.ChangeTracker.Entries<ExternalDocument>()
            .Any(e => e.State is EntityState.Added or EntityState.Modified);

        if (FailDocumentSaves && touchesDocuments)
            throw new DbUpdateException("Simulated database failure.");

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
