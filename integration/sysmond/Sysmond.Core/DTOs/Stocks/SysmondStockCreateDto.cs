namespace Integration.Sysmond.Core.DTOs.Stocks;

/// <summary>Sysmond <c>POST /api/app/stock</c> gövdesi (StockCreateDto).</summary>
public class SysmondStockCreateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>10 = Goods, 20 = Services.</summary>
    public int Type { get; set; } = 10;

    public double? VatPercent { get; set; }
    public double? OtvPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Code { get; set; }
    public int OtvTaxCode { get; set; }
    public string? Gtip { get; set; }
    public Guid? ActId { get; set; }
    public Guid? MeasureUnitId { get; set; }
    public bool StockTrackingEnabled { get; set; } = true;
    public bool StockQuantityControlEnabled { get; set; }
    public SysmondDefaultStockPriceCreateDto? Price { get; set; }
    public IReadOnlyList<SysmondOpeningQuantityDto>? OpeningQuantity { get; set; }
}
