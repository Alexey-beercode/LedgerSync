using LedgerSync.Domain.Common;
using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using LedgerSync.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSync.Infrastructure.Persistence.Configurations;

internal sealed class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.ToTable("sync_logs");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status)
            .HasConversion(new EnumerationConverter<SyncStatus>())
            .HasMaxLength(FieldLimits.EnumerationName);

        builder.Property(s => s.GoogleSheetStatus)
            .HasConversion(new EnumerationConverter<GoogleSheetStatus>())
            .HasMaxLength(FieldLimits.EnumerationName);

        builder.Property(s => s.ErrorMessage).HasMaxLength(FieldLimits.ErrorMessage);
        builder.Property(s => s.GoogleSheetError).HasMaxLength(FieldLimits.ErrorMessage);

        builder.Ignore(s => s.IsFinished);

        // History is always read newest first.
        builder.HasIndex(s => s.StartedAt).IsDescending();

        builder.HasMany(s => s.Errors)
            .WithOne()
            .HasForeignKey(e => e.SyncLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Errors).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic concurrency via PostgreSQL system column xmin.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
