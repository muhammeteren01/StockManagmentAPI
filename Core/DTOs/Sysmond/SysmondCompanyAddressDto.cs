namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond <c>GET /api/app/company-address/{id}/address-by-id</c> <c>CompanyAddressDto</c>.
/// </summary>
public class SysmondCompanyAddressDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Type { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int CountryId { get; set; }
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public string? CountryName { get; set; }
    public string? CityName { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictOther { get; set; }
    public string? DistrictName { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? Room { get; set; }
    public string? Floor { get; set; }
    public string? PostalZone { get; set; }
    public string? Note { get; set; }
    public bool IsDisabled { get; set; }
    public SysmondContactInfoDto? ContactInfoBase { get; set; }
}

/// <summary>Sysmond <c>ApiResultOfCompanyAddressDto</c>.</summary>
public class SysmondCompanyAddressResult
{
    public SysmondCompanyAddressDto? Data { get; set; }
    public object? Status { get; set; }
}
