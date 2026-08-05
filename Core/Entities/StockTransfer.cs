using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Depolar arası stok transferi (başlık kaydı). Transfer edilen ürünler Items'ta tutulur.
/// In_Transit durumundaki stok Inventory'de görünmez; transfer kalemlerinden hesaplanır.
/// </summary>
public class StockTransfer
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Transfer durumu. DB'ye string olarak yazılır (DbContext'te HasConversion ile).</summary>
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Pending;

    public string ReferenceNo { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public DateTime? CompletionDate { get; set; }

    /// <summary>Yoldaki (In_Transit) stoklar inventory tablosunda DEĞİL, bu transferin kalemlerinden (items) hesaplanır.</summary>
    public string? Notes { get; set; }

    public Company Company { get; set; } = null!;
    public Warehouse FromWarehouse { get; set; } = null!;
    public Warehouse ToWarehouse { get; set; } = null!;
    public User User { get; set; } = null!;

    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
