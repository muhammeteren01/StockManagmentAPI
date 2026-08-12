namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ActAddressDto</c>.</summary>
public class SysmondActAddressDto
{
    public Guid Id { get; set; }
    public Guid ActId { get; set; }

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

    public SysmondActAddressContactDto? ContactInfo { get; set; }
}

public class SysmondActAddressContactDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? MainPhone { get; set; }
    public string? MainCellPhone { get; set; }
}

/// <summary>
/// Sysmond act-address list yanıtı.
/// Schema: <c>status</c>+<c>data</c>; dokümantasyon örneği: <c>success</c>+<c>data</c>;
/// bazı listelerde <c>items</c> da gelebilir.
/// </summary>
public class SysmondActAddressListResult
{
    public IReadOnlyList<SysmondActAddressDto>? Data { get; set; }
    public IReadOnlyList<SysmondActAddressDto>? Items { get; set; }
    public object? Status { get; set; }
    public bool? Success { get; set; }

    public IReadOnlyList<SysmondActAddressDto> ResolveItems()
        => (Data is { Count: > 0 } ? Data : null)
           ?? (Items is { Count: > 0 } ? Items : null)
           ?? Data
           ?? Items
           ?? Array.Empty<SysmondActAddressDto>();
}
