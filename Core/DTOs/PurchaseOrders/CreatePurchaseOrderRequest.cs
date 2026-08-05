namespace Core.DTOs.PurchaseOrders;

/// <summary>Yeni satın alma siparişi oluşturma isteği.</summary>
public class CreatePurchaseOrderRequest
{
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
}
