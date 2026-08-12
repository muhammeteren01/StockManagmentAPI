namespace Core.DTOs.Sysmond;

/// <summary>
/// Yerel API: Sysmondax cari güncelleme isteği.
/// Route id = Sysmond act id (Act.ExternalSysmondId).
/// </summary>
public class SysmondUpdateActRequest
{
    /// <summary>ActTypes: 10 Customer, 20 Supplier, 30 CustomerAndSupplier, 40 Carrier.</summary>
    public int? Type { get; set; }

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Title { get; set; }
    public string? ActCode { get; set; }
    public string? VknTckn { get; set; }
    public string? TaxOfficeName { get; set; }
    public string? ActFullAddress { get; set; }

    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public string? CityOther { get; set; }

    public int? MainCurrencyId { get; set; }
    public bool? IsCommunityCompany { get; set; }
    public bool? IsAbroadCustomer { get; set; }

    /// <summary>InvoiceScenarios.</summary>
    public int? Scenario { get; set; }

    public bool? IsDisabled { get; set; }
}
