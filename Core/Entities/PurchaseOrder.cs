using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Ticari belge başlığı: klasik satın alma siparişi veya Sysmond irsaliyesi
/// (<see cref="DocumentType"/>). Kalemler <see cref="Items"/> içindedir.
/// Stok miktarı bu tabloda tutulmaz; hareketler <see cref="StockTransaction"/> ile yazılır.
/// </summary>
public class PurchaseOrder
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>Belge türü. Varsayılan: satın alma siparişi.</summary>
    public PurchaseOrderDocumentType DocumentType { get; set; } = PurchaseOrderDocumentType.PurchaseOrder;

    /// <summary>Klasik PO için zorunlu; irsaliyede cari henüz map edilmemişse null olabilir.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>PO teslim deposu veya irsaliye varsayılan depo; kalem bazlı depo Items'ta olabilir.</summary>
    public Guid? WarehouseId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>PO sipariş no veya irsaliye DocNo.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    /// <summary>Durum. DB'ye string olarak yazılır.</summary>
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    /// <summary>Sysmond despatch direction; klasik PO'da null.</summary>
    public DespatchDirection? Direction { get; set; }

    /// <summary>Sysmond despatch.id — upsert anahtarı.</summary>
    public Guid? ExternalSysmondId { get; set; }

    /// <summary>Sysmond companyPeriodId.</summary>
    public Guid? ExternalSysmondCompanyPeriodId { get; set; }

    /// <summary>Sysmond companyAddressId (seçilmiş şirket adresi).</summary>
    public Guid? ExternalSysmondCompanyAddressId { get; set; }

    /// <summary>Teslim / sevk adresi (Sysmond deliveryAddressCreateDto JSON).</summary>
    public string? DeliveryAddressJson { get; set; }

    /// <summary>Cari adı (Supplier map yokken sync için).</summary>
    public string? ActName { get; set; }

    /// <summary>Cari VKN/TCKN.</summary>
    public string? ActVknTckn { get; set; }

    public DateTime? IssueDate { get; set; }
    public DateTime? ActualDespatchDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public User User { get; set; } = null!;

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
