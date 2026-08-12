using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

/// <summary>act_addresses tablosu Fluent API yapılandırması.</summary>
public class ActAddressConfiguration : IEntityTypeConfiguration<ActAddress>
{
    public void Configure(EntityTypeBuilder<ActAddress> builder)
    {
        builder.ToTable("act_addresses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.ActId).HasColumnName("act_id").IsRequired();
        builder.Property(x => x.ExternalSysmondId).HasColumnName("external_sysmond_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").IsRequired();
        builder.Property(x => x.CountryId).HasColumnName("country_id").IsRequired();
        builder.Property(x => x.CityId).HasColumnName("city_id");
        builder.Property(x => x.CityOther).HasColumnName("city_other").HasMaxLength(100);
        builder.Property(x => x.DistrictId).HasColumnName("district_id");
        builder.Property(x => x.DistrictOther).HasColumnName("district_other").HasMaxLength(100);
        builder.Property(x => x.Street).HasColumnName("street").HasMaxLength(255);
        builder.Property(x => x.BuildingNumber).HasColumnName("building_number").HasMaxLength(50);
        builder.Property(x => x.BuildingName).HasColumnName("building_name").HasMaxLength(100);
        builder.Property(x => x.Room).HasColumnName("room").HasMaxLength(50);
        builder.Property(x => x.Floor).HasColumnName("floor").HasMaxLength(50);
        builder.Property(x => x.PostalZone).HasColumnName("postal_zone").HasMaxLength(20);
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(x => x.CountryName).HasColumnName("country_name").HasMaxLength(100);
        builder.Property(x => x.CityName).HasColumnName("city_name").HasMaxLength(100);
        builder.Property(x => x.DistrictName).HasColumnName("district_name").HasMaxLength(100);
        builder.Property(x => x.IsDisabled).HasColumnName("is_disabled").IsRequired();
        builder.Property(x => x.ContactFirstName).HasColumnName("contact_first_name").HasMaxLength(100);
        builder.Property(x => x.ContactLastName).HasColumnName("contact_last_name").HasMaxLength(100);
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(150);
        builder.Property(x => x.ContactMainPhone).HasColumnName("contact_main_phone").HasMaxLength(50);
        builder.Property(x => x.ContactMainCellPhone).HasColumnName("contact_main_cell_phone").HasMaxLength(50);
        builder.Property(x => x.SyncedAt).HasColumnName("synced_at").IsRequired();

        builder.HasIndex(x => x.ExternalSysmondId).IsUnique();
        builder.HasIndex(x => x.ActId);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Act)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.ActId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
