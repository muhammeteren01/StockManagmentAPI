namespace Core.DTOs.StockTransfers;

/// <summary>Yeni transfer oluşturma isteği.</summary>
public class CreateStockTransferRequest
{
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Guid UserId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<CreateStockTransferItemRequest> Items { get; set; } = new();
}
