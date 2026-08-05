using Core.Abstractions;
using Core.Authorization;
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
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateStockTransactionRequest> _createValidator;

    public StockTransactionService(
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateStockTransactionRequest> createValidator)
    {
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    public async Task<StockTransactionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : StockTransactionMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<StockTransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetAllAsync(cancellationToken);
        return list.Select(StockTransactionMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<StockTransactionResponse>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetByProductIdAsync(productId, cancellationToken);
        return list.Select(StockTransactionMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<StockTransactionResponse>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetByWarehouseIdAsync(warehouseId, cancellationToken);
        return list.Select(StockTransactionMapper.ToResponse).ToList();
    }

    public async Task<StockTransactionResponse> CreateAsync(CreateStockTransactionRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new InvalidOperationException($"Ürün bulunamadı: {request.ProductId}");
        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new InvalidOperationException($"Depo bulunamadı: {request.WarehouseId}");

        if (product.CompanyId != warehouse.CompanyId)
            throw new InvalidOperationException("Ürün ve depo aynı şirkete ait olmalıdır.");

        TenantGuard.EnsureCompanyAccess(_currentUser, product.CompanyId);
        var userId = TenantGuard.RequireUserId(_currentUser);

        var entity = StockTransactionMapper.ToEntity(request, product.CompanyId, userId);
        var inventory = await GetOrCreateInventoryAsync(entity.CompanyId, entity.ProductId, entity.WarehouseId, cancellationToken);
        ApplyQuantityChange(inventory, entity.TransactionType, entity.Quantity);
        inventory.LastUpdated = DateTime.UtcNow;

        await _transactionRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return StockTransactionMapper.ToResponse(entity);
    }

    private async Task<Inventory> GetOrCreateInventoryAsync(
        Guid companyId, Guid productId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        if (inventory is not null)
            return inventory;

        inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
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
