namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>POST /api/app/act/act</c> body (minimal).</summary>
public class SysmondActCreateDto
{
    /// <summary>ActTypes: 40 = Carrier.</summary>
    public int Type { get; set; }

    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? ActCode { get; set; }
    public string? VknTckn { get; set; }
    public int? CountryId { get; set; }
    public int? MainCurrencyId { get; set; }
}
