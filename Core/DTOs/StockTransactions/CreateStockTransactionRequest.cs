using Core.Enums;

namespace Core.DTOs.StockTransactions;

/// <summary>Yeni stok hareketi oluşturma isteği.</summary>
public class CreateStockTransactionRequest
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid UserId { get; set; }
    public Guid? TransferId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public TransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public ReasonCode? ReasonCode { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
}
