using LedgerSync.Domain.Common;
using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSync.Infrastructure.Persistence.Configurations;

internal sealed class SyncLogErrorConfiguration : IEntityTypeConfiguration<SyncLogError>
{
    public void Configure(EntityTypeBuilder<SyncLogError> builder)
    {
        builder.ToTable("sync_log_errors");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ExternalId).HasMaxLength(FieldLimits.ExternalId);
        builder.Property(e => e.Reason).HasMaxLength(FieldLimits.ErrorReason).IsRequired();
    }
}
