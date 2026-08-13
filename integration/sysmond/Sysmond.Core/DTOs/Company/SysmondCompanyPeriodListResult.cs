namespace Integration.Sysmond.Core.DTOs.Company;

/// <summary>Sysmond <c>ApiResultListOfCompanyPeriodDto</c>.</summary>
public class SysmondCompanyPeriodListResult
{
    public IReadOnlyList<SysmondCompanyPeriodDto>? Data { get; set; }
    public object? Status { get; set; }
}
