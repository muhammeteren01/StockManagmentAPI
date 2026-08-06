using Core.Entities;
using Core.Repositories;
using Moq;
using Service.Services;

namespace API.Tests.Services.Inventories;

/// <summary>InventoryService birim testleri için ortak kurulum.</summary>
internal static class InventoryServiceTestHelper
{
    public static InventoryService CreateSut(Mock<IInventoryRepository> repository) =>
        new(repository.Object);

    public static Inventory CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        int quantity = 10,
        DateTime? lastUpdated = null,
        byte[]? rowVersion = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            ProductId = productId ?? Guid.NewGuid(),
            WarehouseId = warehouseId ?? Guid.NewGuid(),
            Quantity = quantity,
            LastUpdated = lastUpdated ?? DateTime.UtcNow,
            RowVersion = rowVersion ?? [1, 2, 3, 4, 5, 6, 7, 8]
        };
}
