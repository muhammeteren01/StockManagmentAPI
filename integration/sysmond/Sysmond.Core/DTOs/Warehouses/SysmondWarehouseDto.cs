namespace Integration.Sysmond.Core.DTOs.Warehouses;

/// <summary>Sysmond <c>GET /api/app/warehouse</c> <c>WarehouseDto</c>.</summary>
public class SysmondWarehouseDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? WarehouseCode { get; set; }
    public bool IsDefault { get; set; }
    public bool? IsActive { get; set; }
}
