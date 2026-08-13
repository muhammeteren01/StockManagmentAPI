namespace Integration.Sysmond.Core.DTOs.Warehouses;

/// <summary>
/// Yerel API: Sysmondax depo oluşturma isteği.
/// Örnek: POST /api/sysmond/warehouses?companyId={guid}
/// </summary>
public class SysmondCreateWarehouseRequest
{
    public string? Name { get; set; }

    /// <summary>Sysmond warehouseCode; yerelde Warehouse.Location.</summary>
    public string? WarehouseCode { get; set; }
}
