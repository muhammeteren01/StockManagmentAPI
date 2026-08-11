using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>PurchaseOrderItem tablosu Fluent API yapılandırması.</summary>
public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("purchase_order_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PurchaseOrderId).HasColumnName("purchase_order_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id");
        builder.Property(x => x.MeasureUnitId).HasColumnName("measure_unit_id");
        builder.Property(x => x.StockPriceId).HasColumnName("stock_price_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100);
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.VatPercent).HasColumnName("vat_percent").HasColumnType("decimal(9,4)");
        builder.Property(x => x.ReceivedQuantity).HasColumnName("received_quantity").IsRequired();

        builder.HasIndex(x => x.ExternalSysmondId)
            .IsUnique()
            .HasFilter("[external_sysmond_id] IS NOT NULL");
        builder.HasIndex(x => x.WarehouseId);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.PurchaseOrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
