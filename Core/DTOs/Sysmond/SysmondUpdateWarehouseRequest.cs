namespace Core.DTOs.Sysmond;

/// <summary>
/// Yerel API: Sysmondax depo güncelleme isteği.
/// Route id = Sysmond warehouse id (Warehouse.ExternalSysmondId).
/// </summary>
public class SysmondUpdateWarehouseRequest
{
    public string? Name { get; set; }

    /// <summary>Sysmond warehouseCode; yerelde Warehouse.Location.</summary>
    public string? WarehouseCode { get; set; }
}
