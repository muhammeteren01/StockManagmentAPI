namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>GET /api/app/user-profile/my-company-periods</c> <c>CompanyPeriodDto</c>.</summary>
public class SysmondCompanyPeriodDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsActive { get; set; }
}
