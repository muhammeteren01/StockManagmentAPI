using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>Inventory tablosu Fluent API yapılandırması.</summary>
public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.LastUpdated).HasColumnName("last_updated").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ExternalSysmondId)
            .IsUnique()
            .HasFilter("[external_sysmond_id] IS NOT NULL");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Inventories)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Inventories)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.Inventories)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
