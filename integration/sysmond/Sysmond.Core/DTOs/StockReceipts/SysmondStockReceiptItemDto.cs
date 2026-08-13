namespace Integration.Sysmond.Core.DTOs.StockReceipts;

/// <summary>Sysmond <c>StockReceiptItemDto</c>.</summary>
public class SysmondStockReceiptItemDto
{
    public Guid Id { get; set; }
    public Guid StockReceiptId { get; set; }
    public Guid StockId { get; set; }
    public string? StockName { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
    public Guid StockPriceId { get; set; }
    public double CurrencyExchangeRate { get; set; }
}
