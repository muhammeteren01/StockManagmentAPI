using Core.DTOs.PurchaseOrders;
using Core.Enums;

namespace API.Tests.Controllers.PurchaseOrders;

/// <summary>PurchaseOrders controller testleri için ortak yardımcılar.</summary>
internal static class PurchaseOrdersTestHelper
{
    public static PurchaseOrderResponse CreateOrderResponse(
        Guid? id = null,
        Guid? companyId = null,
        Guid? supplierId = null,
        Guid? warehouseId = null,
        Guid? userId = null,
        PurchaseOrderStatus status = PurchaseOrderStatus.Pending,
        string orderNumber = "PO-001",
        decimal totalAmount = 500m,
        int itemQuantity = 10,
        decimal unitPrice = 50m,
        int receivedQuantity = 0) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        SupplierId = supplierId ?? Guid.NewGuid(),
        WarehouseId = warehouseId ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        OrderNumber = orderNumber,
        TotalAmount = totalAmount,
        Status = status,
        ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
        Items =
        [
            new PurchaseOrderItemResponse
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = itemQuantity,
                UnitPrice = unitPrice,
                ReceivedQuantity = receivedQuantity
            }
        ]
    };

    public static CreatePurchaseOrderRequest CreateCreateRequest(
        Guid? companyId = null,
        Guid? supplierId = null,
        Guid? warehouseId = null,
        string orderNumber = "PO-001",
        Guid? productId = null,
        int quantity = 10,
        decimal unitPrice = 50m) => new()
    {
        CompanyId = companyId,
        SupplierId = supplierId ?? Guid.NewGuid(),
        WarehouseId = warehouseId ?? Guid.NewGuid(),
        OrderNumber = orderNumber,
        ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
        Items =
        [
            new CreatePurchaseOrderItemRequest
            {
                ProductId = productId ?? Guid.NewGuid(),
                Quantity = quantity,
                UnitPrice = unitPrice
            }
        ]
    };

    public static ReceivePurchaseOrderRequest CreateReceiveRequest(
        Guid? productId = null,
        int quantity = 5) => new()
    {
        ReceivedQuantities = new Dictionary<Guid, int>
        {
            [productId ?? Guid.NewGuid()] = quantity
        }
    };
}
