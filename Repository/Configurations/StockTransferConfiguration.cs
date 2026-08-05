using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>StockTransfer tablosu Fluent API yapılandırması.</summary>
public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("stock_transfers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FromWarehouseId).HasColumnName("from_warehouse_id").IsRequired();
        builder.Property(x => x.ToWarehouseId).HasColumnName("to_warehouse_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceNo).HasColumnName("reference_no").HasMaxLength(100).IsRequired();
        builder.Property(x => x.TransferDate).HasColumnName("transfer_date").IsRequired();
        builder.Property(x => x.CompletionDate).HasColumnName("completion_date");
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.ReferenceNo).IsUnique();

        builder.HasOne(x => x.FromWarehouse)
            .WithMany(x => x.OutgoingTransfers)
            .HasForeignKey(x => x.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToWarehouse)
            .WithMany(x => x.IncomingTransfers)
            .HasForeignKey(x => x.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.StockTransfers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
