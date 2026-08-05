using Core.Enums;

namespace Core.DTOs.StockTransfers;

/// <summary>Transfer kalemi yanıt modeli.</summary>
public class StockTransferItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>Transfer yanıt modeli.</summary>
public class StockTransferResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Guid UserId { get; set; }
    public StockTransferStatus Status { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Notes { get; set; }
    public List<StockTransferItemResponse> Items { get; set; } = new();
}
