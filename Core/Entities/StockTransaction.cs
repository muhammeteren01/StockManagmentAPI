using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Stok hareket kaydı (audit log). Her giriş/çıkış/transfer/düzeltme burada tutulur;
/// Inventory.Quantity bu kayıtlara göre güncellenir. Hareket bir transferden veya
/// satın alma siparişinden kaynaklanıyorsa TransferId / PurchaseOrderId dolu olur.
/// </summary>
public class StockTransaction
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Depo transferinden kaynaklıysa referans</summary>
    public Guid? TransferId { get; set; }

    /// <summary>Mal alımından (PO) kaynaklıysa referans</summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>Hareket türü. DB'ye string olarak yazılır (DbContext'te HasConversion ile).</summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>HER ZAMAN POZİTİF DEĞER. İşlemin yönünü TransactionType belirler (+ veya -)</summary>
    public int Quantity { get; set; }

    /// <summary>Düzeltme/çıkış nedeni (opsiyonel). DB'ye string olarak yazılır.</summary>
    public ReasonCode? ReasonCode { get; set; }

    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }

    public Company Company { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public User User { get; set; } = null!;
    public StockTransfer? Transfer { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
