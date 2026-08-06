using Core.DTOs.Inventories;

namespace API.Tests.Controllers.Inventories;

/// <summary>Inventories controller testleri için ortak yardımcılar.</summary>
internal static class InventoriesTestHelper
{
    public static InventoryResponse CreateInventoryResponse(
        Guid? id = null,
        Guid? companyId = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        int quantity = 10,
        DateTime? lastUpdated = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        ProductId = productId ?? Guid.NewGuid(),
        WarehouseId = warehouseId ?? Guid.NewGuid(),
        Quantity = quantity,
        LastUpdated = lastUpdated ?? DateTime.UtcNow
    };
}
