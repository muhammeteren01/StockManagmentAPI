namespace Integration.Sysmond.Core.DTOs.Acts;

/// <summary>
/// Yerel API: Sysmondax cari oluşturma isteği.
/// Örnek: POST /api/sysmond/acts?companyId={guid}
/// </summary>
public class SysmondCreateActRequest
{
    /// <summary>ActTypes: 10 Customer, 20 Supplier, 30 CustomerAndSupplier, 40 Carrier.</summary>
    public int Type { get; set; } = 20;

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Title { get; set; }
    public string? ActCode { get; set; }
    public string? VknTckn { get; set; }
    public int? CountryId { get; set; }
    public int? MainCurrencyId { get; set; }
}
