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

        builder.Navigation(o => o.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Demo orders. ORD-404 is intentionally absent to show the "unknown order" scenario.
        builder.HasData(
            Seed("6f1f6c1e-0b8a-4a53-9a39-1d5f5d0a0001", "ORD-001", "Alpha Logistics LLP"),
            Seed("6f1f6c1e-0b8a-4a53-9a39-1d5f5d0a0002", "ORD-002", "Beta Construction LLP"),
            Seed("6f1f6c1e-0b8a-4a53-9a39-1d5f5d0a0003", "ORD-003", "Gamma Retail JSC"));
    }

    private static object Seed(string id, string orderNumber, string clientName) => new
    {
        Id = Guid.Parse(id),
        OrderNumber = orderNumber,
        ClientName = clientName,
        CreatedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
    };
}
