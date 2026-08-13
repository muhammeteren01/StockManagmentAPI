namespace Integration.Sysmond.Core.DTOs.Stocks;

/// <summary>
/// Yerel API: Sysmondax'a stok oluşturma isteği.
/// companyId query ile de gelir; body'deki companyId yoksa query kullanılır.
/// openingQuantity: depo başına açılış adedi (Sysmondax o depoya stok ekler).
/// </summary>
public class SysmondCreateStockRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public Guid? CompanyId { get; set; }

    /// <summary>10 = Ürün (Goods), 20 = Hizmet.</summary>
    public int Type { get; set; } = 10;

    public double? VatPercent { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>SKU / stok kodu.</summary>
    public string? Code { get; set; }

    public Guid? MeasureUnitId { get; set; }
    public bool StockTrackingEnabled { get; set; } = true;
    public bool StockQuantityControlEnabled { get; set; }

    public SysmondDefaultStockPriceCreateDto? Price { get; set; }
    public List<SysmondOpeningQuantityDto>? OpeningQuantity { get; set; }
}
