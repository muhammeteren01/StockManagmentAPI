namespace Core.Entities;

/// <summary>
/// Sysmond cari adresi (<c>GET /api/app/act-address?actId=</c>).
/// <see cref="ExternalSysmondId"/> = Sysmond ActAddress.Id.
/// </summary>
public class ActAddress
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ActId { get; set; }

    /// <summary>Sysmond ActAddress.Id.</summary>
    public Guid ExternalSysmondId { get; set; }

    /// <summary>AddressTypes: 10 Business, 20 Home, 30 Other.</summary>
    public int Type { get; set; }

    public int CountryId { get; set; }
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictOther { get; set; }

    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? Room { get; set; }
    public string? Floor { get; set; }
    public string? PostalZone { get; set; }
    public string? Note { get; set; }

    public string? CountryName { get; set; }
    public string? CityName { get; set; }
    public string? DistrictName { get; set; }

    public bool IsDisabled { get; set; }

    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactMainPhone { get; set; }
    public string? ContactMainCellPhone { get; set; }

    public DateTime SyncedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Act Act { get; set; } = null!;
}
