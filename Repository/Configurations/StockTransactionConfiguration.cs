using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>StockTransaction tablosu Fluent API yapılandırması.</summary>
public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("stock_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TransferId).HasColumnName("transfer_id");
        builder.Property(x => x.PurchaseOrderId).HasColumnName("purchase_order_id");
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id");
        builder.Property(x => x.ExternalSysmondDespatchId).HasColumnName("external_sysmond_despatch_id");
        builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ReferenceNo).HasColumnName("reference_no").HasMaxLength(100);
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("nvarchar(max)");
        builder.Property(x => x.TransactionDate).HasColumnName("transaction_date").IsRequired();

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ExternalSysmondId)
            .IsUnique()
            .HasFilter("[external_sysmond_id] IS NOT NULL");
        builder.HasIndex(x => x.ExternalSysmondDespatchId);

        builder.HasOne(x => x.Company)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Transfer)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.StockTransactions)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
