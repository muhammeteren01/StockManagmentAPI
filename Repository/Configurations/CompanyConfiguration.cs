using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>Company tablosu Fluent API yapılandırması.</summary>
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.TaxOffice).HasColumnName("tax_office").HasMaxLength(200);
        builder.Property(x => x.TaxNumber).HasColumnName("tax_number").HasMaxLength(50);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(x => x.Address).HasColumnName("address").HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
