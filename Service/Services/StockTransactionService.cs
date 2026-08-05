using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Stok hareketi oluşturur; Inventory miktarını TransactionType'a göre günceller.</summary>
public class StockTransactionService : GenericService<StockTransaction>, IStockTransactionService
{
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public StockTransactionService(
        IStockTransactionRepository repository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _transactionRepository = repository;
        _inventoryRepository = inventoryRepository;
    }

    public Task<IReadOnlyList<StockTransaction>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _transactionRepository.GetByProductIdAsync(productId, cancellationToken);
    }

    public Task<IReadOnlyList<StockTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return _transactionRepository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
    }

    public override async Task<StockTransaction> CreateAsync(StockTransaction entity, CancellationToken cancellationToken = default)
    {
        if (entity.Quantity <= 0)
            throw new InvalidOperationException("Quantity her zaman pozitif olmalıdır.");

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
