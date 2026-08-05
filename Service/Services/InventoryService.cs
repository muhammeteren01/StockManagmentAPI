using Core.DTOs.Inventories;
using Core.Mappings;
using Core.Repositories;
using Core.Services;

namespace Service.Services;

/// <summary>Inventory sorgu implementasyonu (DTO).</summary>
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;

    public InventoryService(IInventoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Id ile stok satırı getirir.</summary>
    public async Task<InventoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : InventoryMapper.ToResponse(entity);
    }

    /// <summary>Tüm stok satırlarını listeler.</summary>
    public async Task<IReadOnlyList<InventoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(InventoryMapper.ToResponse).ToList();
    }

    /// <summary>Ürün + depo stok satırını getirir.</summary>
    public async Task<InventoryResponse?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        return entity is null ? null : InventoryMapper.ToResponse(entity);
    }

    /// <summary>Depodaki stokları listeler.</summary>
    public async Task<IReadOnlyList<InventoryResponse>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
        return list.Select(InventoryMapper.ToResponse).ToList();
    }

    /// <summary>Ürünün tüm depolardaki stoklarını listeler.</summary>
    public async Task<IReadOnlyList<InventoryResponse>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByProductIdAsync(productId, cancellationToken);
        return list.Select(InventoryMapper.ToResponse).ToList();
    }
}
