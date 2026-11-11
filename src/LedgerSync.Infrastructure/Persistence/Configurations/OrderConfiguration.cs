using LedgerSync.Domain.Common;
using LedgerSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSync.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.OrderNumber).HasMaxLength(FieldLimits.OrderNumber).IsRequired();
        builder.Property(o => o.ClientName).HasMaxLength(FieldLimits.ClientName).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        // Business key: one order per number.
        builder.HasIndex(o => o.OrderNumber).IsUnique();
    }
}
