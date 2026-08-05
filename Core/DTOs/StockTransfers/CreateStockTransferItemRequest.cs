namespace Core.DTOs.StockTransfers;

/// <summary>Transfer kalemi oluşturma isteği.</summary>
public class CreateStockTransferItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
