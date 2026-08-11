using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>PurchaseOrder tablosu Fluent API yapılandırması.</summary>
public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.DocumentType).HasColumnName("document_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(100).IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id");
        builder.Property(x => x.ExternalSysmondCompanyPeriodId).HasColumnName("external_sysmond_company_period_id");
        builder.Property(x => x.ExternalSysmondCompanyAddressId).HasColumnName("external_sysmond_company_address_id");
        builder.Property(x => x.DeliveryAddressJson).HasColumnName("delivery_address_json").HasColumnType("nvarchar(max)");
        builder.Property(x => x.ActName).HasColumnName("act_name").HasMaxLength(255);
        builder.Property(x => x.ActVknTckn).HasColumnName("act_vkn_tckn").HasMaxLength(20);
        builder.Property(x => x.IssueDate).HasColumnName("issue_date");
        builder.Property(x => x.ActualDespatchDate).HasColumnName("actual_despatch_date");
        builder.Property(x => x.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.DocumentType);
        builder.HasIndex(x => x.ExternalSysmondId)
            .IsUnique()
            .HasFilter("[external_sysmond_id] IS NOT NULL");
        builder.HasIndex(x => x.ExternalSysmondCompanyPeriodId);

        builder.HasOne(x => x.Company)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.User)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
