namespace Core.DTOs.PurchaseOrders;

/// <summary>Mal kabulü isteği; ürünId → kabul edilen miktar.</summary>
public class ReceivePurchaseOrderRequest
{
    public Dictionary<Guid, int> ReceivedQuantities { get; set; } = new();
}
