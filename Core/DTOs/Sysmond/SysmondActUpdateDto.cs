namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>PUT /api/app/act/act</c> body (<c>ActUpdateDto</c>).</summary>
public class SysmondActUpdateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Type { get; set; }

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
    public bool IsCommunityCompany { get; set; }
    public bool IsAbroadCustomer { get; set; }
    public int Scenario { get; set; }
    public bool IsDisabled { get; set; }
}
