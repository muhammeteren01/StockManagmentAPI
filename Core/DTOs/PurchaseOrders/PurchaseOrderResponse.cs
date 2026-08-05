using Core.Enums;

namespace Core.DTOs.PurchaseOrders;

/// <summary>Sipariş kalemi yanıt modeli.</summary>
public class PurchaseOrderItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReceivedQuantity { get; set; }
}

/// <summary>Satın alma siparişi yanıt modeli.</summary>
public class PurchaseOrderResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderItemResponse> Items { get; set; } = new();
}
