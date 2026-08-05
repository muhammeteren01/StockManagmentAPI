using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Inventory iş kuralları implementasyonu; stok sorguları.</summary>
public class InventoryService : GenericService<Inventory>, IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(
        IInventoryRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<Inventory> validator)
        : base(repository, unitOfWork, validator)
    {
        _inventoryRepository = repository;
    }

    /// <summary>Ürün ve depo çiftine göre stok satırını getirir.</summary>
    public Task<Inventory?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return _inventoryRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
    }

    /// <summary>Belirli depodaki tüm stok satırlarını listeler.</summary>
    public Task<IReadOnlyList<Inventory>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return _inventoryRepository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
    }

    /// <summary>Belirli ürünün tüm depolardaki stoklarını listeler.</summary>
    public Task<IReadOnlyList<Inventory>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _inventoryRepository.GetByProductIdAsync(productId, cancellationToken);
    }
}
