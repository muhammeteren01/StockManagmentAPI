using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Ürün kartı. Fiyat, SKU/barkod ve minimum stok seviyesi gibi tanım bilgilerini tutar.
/// Gerçek stok miktarı bu tabloda değil, depo bazında Inventory tablosunda tutulur.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid SupplierId { get; set; }

    /// <summary>Şirket bazında unique olmalı</summary>
    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int MinStockLevel { get; set; }

    /// <summary>Ürün durumu. DB'ye string olarak yazılır (DbContext'te HasConversion ile).</summary>
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public DateTime CreatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
