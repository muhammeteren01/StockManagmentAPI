using Core.DTOs.StockTransactions;
using Core.Entities;
using Core.Enums;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Stok hareketi oluşturur; Inventory'yi günceller (DTO).</summary>
public class StockTransactionService : IStockTransactionService
{
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateStockTransactionRequest> _createValidator;

    public StockTransactionService(
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateStockTransactionRequest> createValidator)
    {
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    /// <summary>Id ile stok hareketi getirir.</summary>
    public async Task<StockTransactionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : StockTransactionMapper.ToResponse(entity);
    }

    /// <summary>Tüm stok hareketlerini listeler.</summary>
    public async Task<IReadOnlyList<StockTransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetAllAsync(cancellationToken);
        return list.Select(StockTransactionMapper.ToResponse).ToList();
    }

    /// <summary>Ürüne ait hareketleri listeler.</summary>
    public async Task<IReadOnlyList<StockTransactionResponse>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetByProductIdAsync(productId, cancellationToken);
        return list.Select(StockTransactionMapper.ToResponse).ToList();
    }

    /// <summary>Depoya ait hareketleri listeler.</summary>
    public async Task<IReadOnlyList<StockTransactionResponse>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
        return list.Select(StockTransactionMapper.ToResponse).ToList();
    }

    /// <summary>Stok hareketi kaydı oluşturur ve Inventory miktarını günceller.</summary>
    public async Task<StockTransactionResponse> CreateAsync(CreateStockTransactionRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var entity = StockTransactionMapper.ToEntity(request);
        var inventory = await GetOrCreateInventoryAsync(entity.ProductId, entity.WarehouseId, cancellationToken);
        ApplyQuantityChange(inventory, entity.TransactionType, entity.Quantity);
        inventory.LastUpdated = DateTime.UtcNow;

        await _transactionRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return StockTransactionMapper.ToResponse(entity);
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
