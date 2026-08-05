using Core.DTOs.Inventories;

namespace Core.Services;

/// <summary>Inventory sorgu servisi (DTO tabanlı).</summary>
public interface IInventoryService
{
    Task<InventoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InventoryResponse?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryResponse>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryResponse>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
