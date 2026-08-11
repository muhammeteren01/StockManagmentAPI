namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond <c>GET /api/app/despatch-query/{id}/despatch-delivery-address</c>
/// <c>DespatchDeliveryAddressDto</c>.
/// </summary>
public class SysmondDespatchDeliveryAddressDto
{
    public Guid Id { get; set; }
    public Guid DespatchId { get; set; }
    public int Type { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
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
    public bool IsDisabled { get; set; }
    public SysmondContactInfoDto? Contact { get; set; }
}

/// <summary>Sysmond <c>ContactInfoDto</c> (adres içi iletişim).</summary>
public class SysmondContactInfoDto
{
    public string? Title { get; set; }
    public string? Greeting { get; set; }
    public string? FirstName { get; set; }
    public string? MidName { get; set; }
    public string? LastName { get; set; }
    public bool? IsPrimaryContact { get; set; }
    public string? Email { get; set; }
    public string? MainCellPhone { get; set; }
    public string? CellPhone { get; set; }
    public string? MainPhone { get; set; }
    public string? Phone { get; set; }
    public string? MainFax { get; set; }
    public string? Website { get; set; }
    public string? Fax { get; set; }
    public string? VknTckn { get; set; }
}

/// <summary>Sysmond <c>ApiResultOfDespatchDeliveryAddressDto</c>.</summary>
public class SysmondDespatchDeliveryAddressResult
{
    public SysmondDespatchDeliveryAddressDto? Data { get; set; }
    public object? Status { get; set; }
}
