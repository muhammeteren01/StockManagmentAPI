using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Satın alma siparişi (başlık kaydı). Tedarikçiye verilen siparişi ve mal kabulünün
/// yapılacağı depoyu tutar. Sipariş kalemleri Items'tadır; mal kabulünde
/// stock_transactions tablosuna IN kaydı düşülür.
/// </summary>
public class PurchaseOrder
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }

    /// <summary>Siparişin teslim edileceği (mal kabulü yapılacak) depo</summary>
    public Guid WarehouseId { get; set; }

    public Guid UserId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    /// <summary>Sipariş durumu. DB'ye string olarak yazılır (DbContext'te HasConversion ile).</summary>
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public User User { get; set; } = null!;

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
