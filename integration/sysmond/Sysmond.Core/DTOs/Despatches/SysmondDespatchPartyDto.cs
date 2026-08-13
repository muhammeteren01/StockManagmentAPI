namespace Integration.Sysmond.Core.DTOs.Despatches;

/// <summary>
/// Sysmond <c>GET /api/app/despatch-party</c> <c>DespatchPartyDto</c>
/// (cari taraf + gömülü adres alanları).
/// Type: 10 DeliveryCustomer, 20 BuyerCustomer, 30 SellerSupplier, 40 OriginatorCustomer.
/// </summary>
public class SysmondDespatchPartyDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public Guid? ActId { get; set; }
    public string? ActVknTckn { get; set; }
    public string? ActName { get; set; }
    public string? ActTaxOfficeName { get; set; }
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
    public string? PersonFirstName { get; set; }
    public string? PersonLastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

/// <summary>Sysmond <c>ApiResultListOfDespatchPartyDto</c>.</summary>
public class SysmondDespatchPartyListResult
{
    public IReadOnlyList<SysmondDespatchPartyDto>? Data { get; set; }
    public object? Status { get; set; }
}
