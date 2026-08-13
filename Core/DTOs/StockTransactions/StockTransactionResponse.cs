using Core.Enums;

namespace Core.DTOs.StockTransactions;

/// <summary>Stok hareketi yanıt modeli.</summary>
public class StockTransactionResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid UserId { get; set; }
    public Guid? TransferId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>In=Giriş, Out=Çıkış, TransferOut/In=Transfer, Adjustment=Düzeltme.</summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>Türkçe etiket: Giriş / Çıkış / Transfer Çıkış / Transfer Giriş / Düzeltme.</summary>
    public string TransactionTypeLabel { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public ReasonCode? ReasonCode { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }

    public Guid? ExternalSysmondId { get; set; }
    public Guid? ExternalSysmondReceiptId { get; set; }
}
