namespace Integration.Sysmond.Core.DTOs.Stocks;

/// <summary>Sysmond stock-query <c>StockPriceDto</c> özeti.</summary>
public class SysmondStockPriceDto
{
    public Guid Id { get; set; }
    public Guid StockId { get; set; }
    public int CurrencyId { get; set; }
    public double UnitPrice { get; set; }
    public string? Description { get; set; }
    public Guid StockPriceTypeId { get; set; }
    public Guid MeasureUnitId { get; set; }
    public bool IsDefaultMeasureUnitPrice { get; set; }
    public string? StockName { get; set; }
    public string? CurrencyName { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencySymbol { get; set; }
    public string? MeasureUnitName { get; set; }
    public string? MeasureUnitAbbreviation { get; set; }
    public string? StockPriceTypeName { get; set; }
    public bool IsDefault { get; set; }
}
