namespace Core.Entities;

/// <summary>
/// Belge kalemi: PO satırı veya irsaliye ürün/hizmet satırı.
/// Stok etkisi <see cref="StockTransaction"/> ile ayrıca yazılır.
/// </summary>
public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>Kalem deposu (Sysmond despatch-item.warehouseId); yoksa null.</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Sysmond despatch-item.id.</summary>
    public Guid? ExternalSysmondId { get; set; }

    /// <summary>Sysmond measureUnitId.</summary>
    public Guid? MeasureUnitId { get; set; }

    /// <summary>Sysmond stockPriceId.</summary>
    public Guid? StockPriceId { get; set; }

    /// <summary>Sysmond satır adı / kod (ürün kartından bağımsız etiket).</summary>
    public string? Name { get; set; }
    public string? Code { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? VatPercent { get; set; }

    /// <summary>Klasik PO mal kabul takibi; irsaliyede genelde 0.</summary>
    public int ReceivedQuantity { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Warehouse? Warehouse { get; set; }
}
