using Core.DTOs.PurchaseOrders;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>PurchaseOrder entity ↔ DTO dönüşümleri.</summary>
public static class PurchaseOrderMapper
{
    public static PurchaseOrder ToEntity(CreatePurchaseOrderRequest request, Guid companyId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        SupplierId = request.SupplierId,
        WarehouseId = request.WarehouseId,
        UserId = userId,
        OrderNumber = request.OrderNumber,
        ExpectedDeliveryDate = request.ExpectedDeliveryDate,
        Status = PurchaseOrderStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        Items = request.Items.Select(i => new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            ReceivedQuantity = 0
        }).ToList(),
        TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
    };

    public static PurchaseOrderResponse ToResponse(PurchaseOrder entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        SupplierId = entity.SupplierId,
        WarehouseId = entity.WarehouseId,
        UserId = entity.UserId,
        OrderNumber = entity.OrderNumber,
        TotalAmount = entity.TotalAmount,
        Status = entity.Status,
        ExpectedDeliveryDate = entity.ExpectedDeliveryDate,
        CreatedAt = entity.CreatedAt,
        Items = entity.Items?.Select(i => new PurchaseOrderItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            ReceivedQuantity = i.ReceivedQuantity
        }).ToList() ?? []
    };
}
