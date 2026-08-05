using Core.DTOs.Inventories;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Inventory entity ↔ DTO dönüşümleri.</summary>
public static class InventoryMapper
{
    public static InventoryResponse ToResponse(Inventory entity) => new()
    {
        Id = entity.Id,
        ProductId = entity.ProductId,
        WarehouseId = entity.WarehouseId,
        Quantity = entity.Quantity,
        LastUpdated = entity.LastUpdated
    };
}
