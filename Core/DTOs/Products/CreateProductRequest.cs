using Core.Enums;

namespace Core.DTOs.Products;

/// <summary>Yeni ürün oluşturma isteği. CompanyId yalnızca SuperAdmin için gerekir.</summary>
public class CreateProductRequest
{
    public Guid? CompanyId { get; set; }

    /// <summary>Opsiyonel; Sysmond senkronunda kullanılmaz.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Opsiyonel; tedarikçi bağlantısı sonra eklenir.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>10 = Goods, 20 = Services (varsayılan Goods).</summary>
    public ProductType Type { get; set; } = ProductType.Goods;

    public Guid? MeasureUnitId { get; set; }
    public Guid? ExternalSysmondId { get; set; }

    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Alış birim fiyatı.</summary>
    public decimal UnitPrice { get; set; }

    public int? PurchaseCurrencyId { get; set; }

    /// <summary>Satış birim fiyatı.</summary>
    public decimal SellingPrice { get; set; }

    public int? SaleCurrencyId { get; set; }

    public int MinStockLevel { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
}
