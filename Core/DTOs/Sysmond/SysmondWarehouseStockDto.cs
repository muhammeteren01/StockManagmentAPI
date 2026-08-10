namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond <c>GET /api/app/warehouse-stock</c> <c>WarehouseStockDto</c>.
/// Not: OpenAPI'da miktar yoktur; yalnızca warehouse–stock bağı ve satır id'si.
/// </summary>
public class SysmondWarehouseStockDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockId { get; set; }
    public double? CriticalStockLevel { get; set; }
    public string? WarehouseName { get; set; }
    public string? StockName { get; set; }
    public string? StockCode { get; set; }
}
