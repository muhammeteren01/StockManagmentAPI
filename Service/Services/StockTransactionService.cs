using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Stok hareketi oluşturur; Inventory miktarını TransactionType'a göre günceller.</summary>
public class StockTransactionService : GenericService<StockTransaction>, IStockTransactionService
{
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public StockTransactionService(
        IStockTransactionRepository repository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        IValidator<StockTransaction> validator)
        : base(repository, unitOfWork, validator)
    {
        _transactionRepository = repository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>Ürüne ait stok hareketlerini listeler.</summary>
    public Task<IReadOnlyList<StockTransaction>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _transactionRepository.GetByProductIdAsync(productId, cancellationToken);
    }

    /// <summary>Depoya ait stok hareketlerini listeler.</summary>
    public Task<IReadOnlyList<StockTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return _transactionRepository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
    }

    /// <summary>Stok hareketi kaydı oluşturur ve Inventory miktarını günceller.</summary>
    public override async Task<StockTransaction> CreateAsync(StockTransaction entity, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(entity, cancellationToken);

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (entity.TransactionDate == default)
            entity.TransactionDate = DateTime.UtcNow;

        var inventory = await GetOrCreateInventoryAsync(entity.ProductId, entity.WarehouseId, cancellationToken);
        ApplyQuantityChange(inventory, entity.TransactionType, entity.Quantity);
        inventory.LastUpdated = DateTime.UtcNow;

        await _transactionRepository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>Ürün-depo stok satırını bulur; yoksa sıfır miktarla oluşturur.</summary>
    private async Task<Inventory> GetOrCreateInventoryAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        if (inventory is not null)
            return inventory;

        inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 0,
            LastUpdated = DateTime.UtcNow
        };

        await _inventoryRepository.AddAsync(inventory, cancellationToken);
        return inventory;
    }

    /// <summary>Hareket tipine göre stok miktarını artırır veya azaltır.</summary>
    private static void ApplyQuantityChange(Inventory inventory, TransactionType type, int quantity)
    {
        var delta = type switch
        {
            TransactionType.In or TransactionType.TransferIn => quantity,
            TransactionType.Out or TransactionType.TransferOut or TransactionType.Adjustment => -quantity,
            _ => throw new InvalidOperationException($"Desteklenmeyen hareket tipi: {type}")
        };

        if (inventory.Quantity + delta < 0)
            throw new InvalidOperationException("Yetersiz stok.");

        inventory.Quantity += delta;
    }
}
