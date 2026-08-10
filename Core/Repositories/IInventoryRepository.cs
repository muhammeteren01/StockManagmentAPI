using Core.Entities;

namespace Core.Repositories;

/// <summary>Inventory entity'sine ait veri erişim işlemleri.</summary>
public interface IInventoryRepository : IGenericRepository<Inventory>
{
    Task<Inventory?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<Inventory?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
