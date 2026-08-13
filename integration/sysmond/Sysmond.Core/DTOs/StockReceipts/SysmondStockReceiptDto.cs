namespace Integration.Sysmond.Core.DTOs.StockReceipts;

/// <summary>Sysmond <c>StockReceiptDto</c> — stok fişi (giriş/çıkış/transfer).</summary>
public class SysmondStockReceiptDto
{
    public Guid Id { get; set; }
    public string? DocNo { get; set; }
    public Guid CompanyPeriodId { get; set; }
    public string? Description { get; set; }

    /// <summary>10 Entry, 20 Exit, 30 Adjustment, 40 Loss, 50 Transfer.</summary>
    public int Type { get; set; }

    public string? TypeName { get; set; }
    public DateTime? TransactionDate { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public string? TargetWarehouseName { get; set; }
    public bool IsDraft { get; set; }
}
