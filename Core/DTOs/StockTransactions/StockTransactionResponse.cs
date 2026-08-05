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
    public TransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public ReasonCode? ReasonCode { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }
}
