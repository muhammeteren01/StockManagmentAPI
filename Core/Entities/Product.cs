using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Ürün kartı. Fiyat, SKU/barkod ve minimum stok seviyesi gibi tanım bilgilerini tutar.
/// Gerçek stok miktarı bu tabloda değil, depo bazında Inventory tablosunda tutulur.
/// CategoryId / SupplierId opsiyoneldir; Sysmond senkronunda Type kullanılır, tedarikçi sipariş tarafında bağlanır.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>Opsiyonel kategori; Sysmond senkronunda kullanılmaz.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Opsiyonel tedarikçi; ürün-tedarikçi bağlantısı sonra eklenir.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>Sysmond StockTypes: 10 Goods, 20 Services.</summary>
    public ProductType Type { get; set; } = ProductType.Goods;

    /// <summary>Sysmond stock.id — upsert anahtarı.</summary>
    public Guid? ExternalSysmondId { get; set; }

    /// <summary>Sysmond measureUnitId (katalog id'si olduğu gibi saklanır).</summary>
    public Guid? MeasureUnitId { get; set; }

    /// <summary>Şirket bazında unique olmalı</summary>
    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Alış birim fiyatı (Sysmond purchaseUnitPrice).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Alış para birimi id (Sysmond purchaseCurrencyId; katalog çözümü yok).</summary>
    public int? PurchaseCurrencyId { get; set; }

    /// <summary>Satış birim fiyatı (Sysmond saleUnitPrice).</summary>
    public decimal SellingPrice { get; set; }

    /// <summary>Satış para birimi id (Sysmond saleCurrencyId; katalog çözümü yok).</summary>
    public int? SaleCurrencyId { get; set; }

    public int MinStockLevel { get; set; }

    /// <summary>Ürün durumu. DB'ye string olarak yazılır (DbContext'te HasConversion ile).</summary>
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public DateTime CreatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Category? Category { get; set; }
    public Supplier? Supplier { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
