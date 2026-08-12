namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>PUT /api/app/warehouse</c> body (<c>WarehouseUpdateDto</c>).</summary>
public class SysmondWarehouseUpdateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? WarehouseCode { get; set; }
}
