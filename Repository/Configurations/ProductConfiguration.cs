using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>Product tablosu Fluent API yapılandırması.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id");
        builder.Property(x => x.MeasureUnitId).HasColumnName("measure_unit_id");
        builder.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Barcode).HasColumnName("barcode").HasMaxLength(100);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.PurchaseCurrencyId).HasColumnName("purchase_currency_id");
        builder.Property(x => x.SellingPrice).HasColumnName("selling_price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.SaleCurrencyId).HasColumnName("sale_currency_id");
        builder.Property(x => x.MinStockLevel).HasColumnName("min_stock_level").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
        builder.HasIndex(x => x.ExternalSysmondId)
            .IsUnique()
            .HasFilter("[external_sysmond_id] IS NOT NULL");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
