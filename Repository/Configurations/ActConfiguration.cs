using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>acts tablosu Fluent API yapılandırması.</summary>
public class ActConfiguration : IEntityTypeConfiguration<Act>
{
    public void Configure(EntityTypeBuilder<Act> builder)
    {
        builder.ToTable("acts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Surname).HasColumnName("surname").HasMaxLength(255);
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(255);
        builder.Property(x => x.ActCode).HasColumnName("act_code").HasMaxLength(100);
        builder.Property(x => x.VknTckn).HasColumnName("vkn_tckn").HasMaxLength(20);
        builder.Property(x => x.TaxOfficeName).HasColumnName("tax_office_name").HasMaxLength(150);
        builder.Property(x => x.ActFullAddress).HasColumnName("act_full_address").HasMaxLength(500);
        builder.Property(x => x.CountryId).HasColumnName("country_id");
        builder.Property(x => x.CityId).HasColumnName("city_id");
        builder.Property(x => x.CityOther).HasColumnName("city_other").HasMaxLength(100);
        builder.Property(x => x.Scenario).HasColumnName("scenario").IsRequired();
        builder.Property(x => x.IsDisabled).HasColumnName("is_disabled").IsRequired();
        builder.Property(x => x.IsLocked).HasColumnName("is_locked").IsRequired();
        builder.Property(x => x.IsAbroadCustomer).HasColumnName("is_abroad_customer").IsRequired();
        builder.Property(x => x.ParentActExternalId).HasColumnName("parent_act_external_id");
        builder.Property(x => x.SyncedAt).HasColumnName("synced_at").IsRequired();

        builder.HasIndex(x => x.ExternalSysmondId).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.Type });

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Acts)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
