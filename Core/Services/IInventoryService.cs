using Core.Entities;

namespace Core.Services;

/// <summary>Inventory iş kuralları; depo bazlı güncel stok sorgulama ve güncelleme.</summary>
public interface IInventoryService : IGenericService<Inventory>
{
    Task<Inventory?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
