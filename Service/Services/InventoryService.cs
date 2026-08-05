using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Inventory iş kuralları implementasyonu; stok sorguları.</summary>
public class InventoryService : GenericService<Inventory>, IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _inventoryRepository = repository;
    }

    public Task<Inventory?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return _inventoryRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
    }

    public Task<IReadOnlyList<Inventory>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return _inventoryRepository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
    }

    public Task<IReadOnlyList<Inventory>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _inventoryRepository.GetByProductIdAsync(productId, cancellationToken);
    }
}
