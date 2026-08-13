namespace Integration.Sysmond.Core.DTOs.Sync;

/// <summary>Sysmond stock-receipt (giriş/çıkış/transfer) → StockTransaction senkron sonucu.</summary>
public class SysmondStockTransactionSyncResult
{
    public int ReceiptsFetched { get; set; }
    public int ItemsFetched { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
    public int SkippedDraft { get; set; }
    public int SkippedMissingProduct { get; set; }
    public int SkippedMissingWarehouse { get; set; }
    public int Failed { get; set; }
    public int FailedDeletes { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
