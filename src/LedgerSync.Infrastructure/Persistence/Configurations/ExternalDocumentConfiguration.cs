using LedgerSync.Domain.Common;
using LedgerSync.Domain.Entities;
using LedgerSync.Domain.Enumerations;
using LedgerSync.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSync.Infrastructure.Persistence.Configurations;

internal sealed class ExternalDocumentConfiguration : IEntityTypeConfiguration<ExternalDocument>
{
    public void Configure(EntityTypeBuilder<ExternalDocument> builder)
    {
        builder.ToTable("external_documents", table =>
            table.HasCheckConstraint("ck_external_documents_amount_non_negative", "amount >= 0"));

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.ExternalId).HasMaxLength(FieldLimits.ExternalId).IsRequired();
        builder.Property(d => d.OrderNumber).HasMaxLength(FieldLimits.OrderNumber).IsRequired();
        builder.Property(d => d.DocumentNumber).HasMaxLength(FieldLimits.DocumentNumber).IsRequired();
        builder.Property(d => d.Currency).HasMaxLength(FieldLimits.CurrencyCode).IsFixedLength().IsRequired();
        builder.Property(d => d.Amount).HasPrecision(18, 2);

        builder.Property(d => d.DocumentType)
            .HasConversion(new EnumerationConverter<DocumentType>())
            .HasMaxLength(FieldLimits.EnumerationName);

        builder.Property(d => d.Status)
            .HasConversion(new EnumerationConverter<DocumentStatus>())
            .HasMaxLength(FieldLimits.EnumerationName);

        // Deduplication guarantee at the database level, independent of application code.
        builder.HasIndex(d => d.ExternalId).IsUnique();

        // Fast lookup for GET /api/orders/{orderNumber}/documents.
        builder.HasIndex(d => d.OrderNumber);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
