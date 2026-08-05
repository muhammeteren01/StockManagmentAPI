using Core.Abstractions;
using Core.Authorization;
using Core.DTOs.StockTransfers;
using Core.Entities;
using Core.Enums;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Depolar arası transfer yönetir (DTO).</summary>
public class StockTransferService : IStockTransferService
{
    private readonly IStockTransferRepository _transferRepository;
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateStockTransferRequest> _createValidator;

    public StockTransferService(
        IStockTransferRepository transferRepository,
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateStockTransferRequest> createValidator)
    {
        _transferRepository = transferRepository;
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    public async Task<StockTransferResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken);
        return entity is null ? null : StockTransferMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<StockTransferResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _transferRepository.GetAllAsync(cancellationToken);
        return list.Select(StockTransferMapper.ToResponse).ToList();
    }

    public async Task<StockTransferResponse> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        if (request.FromWarehouseId == request.ToWarehouseId)
            throw new InvalidOperationException("Kaynak ve hedef depo farklı olmalıdır.");

        var fromWarehouse = await _warehouseRepository.GetByIdAsync(request.FromWarehouseId, cancellationToken)
            ?? throw new InvalidOperationException($"Kaynak depo bulunamadı: {request.FromWarehouseId}");
        var toWarehouse = await _warehouseRepository.GetByIdAsync(request.ToWarehouseId, cancellationToken)
            ?? throw new InvalidOperationException($"Hedef depo bulunamadı: {request.ToWarehouseId}");

        if (fromWarehouse.CompanyId != toWarehouse.CompanyId)
            throw new InvalidOperationException("Kaynak ve hedef depolar aynı şirkete ait olmalıdır.");

        TenantGuard.EnsureCompanyAccess(_currentUser, fromWarehouse.CompanyId);
        var userId = TenantGuard.RequireUserId(_currentUser);

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Ürün bulunamadı: {item.ProductId}");
            if (product.CompanyId != fromWarehouse.CompanyId)
                throw new InvalidOperationException($"Ürün farklı bir şirkete ait: {item.ProductId}");
        }

        var entity = StockTransferMapper.ToEntity(request, fromWarehouse.CompanyId, userId);
        await _transferRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return StockTransferMapper.ToResponse(entity);
    }

    public async Task StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer bulunamadı: {id}");

        if (transfer.Status != StockTransferStatus.Pending)
            throw new InvalidOperationException("Sadece Pending transferler başlatılabilir.");

        foreach (var item in transfer.Items)
        {
            await ApplyStockChangeAsync(
                transfer.CompanyId, item.ProductId, transfer.FromWarehouseId, transfer.UserId,
                TransactionType.TransferOut, item.Quantity, transfer.Id, cancellationToken);
        }

        transfer.Status = StockTransferStatus.InTransit;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer bulunamadı: {id}");

        if (transfer.Status != StockTransferStatus.InTransit)
            throw new InvalidOperationException("Sadece InTransit transferler tamamlanabilir.");

        foreach (var item in transfer.Items)
        {
            await ApplyStockChangeAsync(
                transfer.CompanyId, item.ProductId, transfer.ToWarehouseId, transfer.UserId,
                TransactionType.TransferIn, item.Quantity, transfer.Id, cancellationToken);
        }

        transfer.Status = StockTransferStatus.Completed;
        transfer.CompletionDate = DateTime.UtcNow;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer bulunamadı: {id}");

        if (transfer.Status is StockTransferStatus.Completed or StockTransferStatus.Cancelled)
            throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş transfer iptal edilemez.");

        if (transfer.Status == StockTransferStatus.InTransit)
        {
            foreach (var item in transfer.Items)
            {
                await ApplyStockChangeAsync(
                    transfer.CompanyId, item.ProductId, transfer.FromWarehouseId, transfer.UserId,
                    TransactionType.TransferIn, item.Quantity, transfer.Id, cancellationToken);
            }
        }

        transfer.Status = StockTransferStatus.Cancelled;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyStockChangeAsync(
        Guid companyId, Guid productId, Guid warehouseId, Guid userId, TransactionType type,
        int quantity, Guid transferId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        if (inventory is null)
        {
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
        }

        var delta = type is TransactionType.TransferIn ? quantity : -quantity;
        if (inventory.Quantity + delta < 0)
            throw new InvalidOperationException("Yetersiz stok.");

        inventory.Quantity += delta;
        inventory.LastUpdated = DateTime.UtcNow;

        await _transactionRepository.AddAsync(new StockTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            UserId = userId,
            TransferId = transferId,
            TransactionType = type,
            Quantity = quantity,
            TransactionDate = DateTime.UtcNow
        }, cancellationToken);
    }
}
