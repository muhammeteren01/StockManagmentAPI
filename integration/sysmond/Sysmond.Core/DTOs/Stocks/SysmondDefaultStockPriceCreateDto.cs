namespace Integration.Sysmond.Core.DTOs.Stocks;

/// <summary>Sysmond <c>DefaultStockPriceCreateDto</c>.</summary>
public class SysmondDefaultStockPriceCreateDto
{
    public int SaleCurrencyId { get; set; }
    public double SaleUnitPrice { get; set; }
    public int PurchaseCurrencyId { get; set; }
    public double PurchaseUnitPrice { get; set; }
    public string? Description { get; set; }
    public Guid MeasureUnitId { get; set; }
}
