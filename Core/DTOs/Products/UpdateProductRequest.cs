using Core.Enums;

namespace Core.DTOs.Products;

/// <summary>Ürün güncelleme isteği.</summary>
public class UpdateProductRequest
{
    public Guid? CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public ProductType Type { get; set; } = ProductType.Goods;
    public Guid? MeasureUnitId { get; set; }
    public Guid? ExternalSysmondId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int? PurchaseCurrencyId { get; set; }
    public decimal SellingPrice { get; set; }
    public int? SaleCurrencyId { get; set; }
    public int MinStockLevel { get; set; }
    public ProductStatus Status { get; set; }
}
