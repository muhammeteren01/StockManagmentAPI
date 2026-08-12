namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ActDto</c> (act-query senkron için gereken alanlar).</summary>
public class SysmondActDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>ActTypes: 10 Customer, 20 Supplier, 30 CustomerAndSupplier, 40 Carrier.</summary>
    public int Type { get; set; }

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Title { get; set; }
    public string? LegalName { get; set; }
    public string? ActCode { get; set; }
    public string? VknTckn { get; set; }
    public string? TaxOfficeName { get; set; }
    public string? ActFullAddress { get; set; }

    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public string? CityOther { get; set; }

    public int Scenario { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsLocked { get; set; }
    public bool IsAbroadCustomer { get; set; }
    public Guid? ParentActId { get; set; }
    public string? TypeName { get; set; }
}

/// <summary>Sysmond <c>ApiResultPagedOfActDto</c>.</summary>
public class SysmondActPagedResult
{
    public IReadOnlyList<SysmondActDto>? Items { get; set; }
    public long TotalCount { get; set; }
    public object? Status { get; set; }
}
