namespace Integration.Sysmond.Core.DTOs.Stocks;

/// <summary>
/// Yerel API: Sysmondax stok güncelleme isteği.
/// Route/body id = Sysmond stock id (Product.ExternalSysmondId).
/// Not: OpenAPI StockUpdateDto price/openingQuantity içermez; miktar inventory sync ile gelir.
/// </summary>
public class SysmondUpdateStockRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }

    /// <summary>10 = Ürün, 20 = Hizmet.</summary>
    public int Type { get; set; } = 10;

    public double? VatPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Code { get; set; }
    public Guid? MeasureUnitId { get; set; }
    public bool StockTrackingEnabled { get; set; } = true;
    public bool StockQuantityControlEnabled { get; set; }
}
