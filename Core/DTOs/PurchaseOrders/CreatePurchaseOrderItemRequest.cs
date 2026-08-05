namespace Core.DTOs.PurchaseOrders;

/// <summary>Sipariş kalemi oluşturma isteği.</summary>
public class CreatePurchaseOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
