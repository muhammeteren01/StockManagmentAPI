namespace Integration.Sysmond.Core.DTOs.Warehouses;

/// <summary>Sysmond <c>POST /api/app/warehouse</c> body (<c>WarehouseCreateDto</c>).</summary>
public class SysmondWarehouseCreateDto
{
    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? WarehouseCode { get; set; }
}
