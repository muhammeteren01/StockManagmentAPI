namespace Integration.Sysmond.Core.DTOs.Inventory;

/// <summary>
/// Sysmond <c>GET /api/app/stock/balance</c> <c>StockBalanceDto</c>.
/// Miktar kaynağı: <see cref="Rem"/> (kalan bakiye).
/// </summary>
public class SysmondStockBalanceDto
{
    public Guid? CompanyPeriodId { get; set; }
    public Guid MeasureUnitId { get; set; }
    public string? MeasureUnitName { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid StockId { get; set; }
    public string? StockName { get; set; }
    public double QuantityIn { get; set; }
    public double QuantityOut { get; set; }

    /// <summary>Depo+stok kalan miktar (Inventory.Quantity).</summary>
    public double Rem { get; set; }
}
