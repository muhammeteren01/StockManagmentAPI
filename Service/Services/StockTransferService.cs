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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateStockTransferRequest> _createValidator;

    public StockTransferService(
        IStockTransferRepository transferRepository,
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateStockTransferRequest> createValidator)
    {
        _transferRepository = transferRepository;
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    /// <summary>Id ile transferi (kalemler dahil) getirir.</summary>
    public async Task<StockTransferResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken);
        return entity is null ? null : StockTransferMapper.ToResponse(entity);
    }

    /// <summary>Tüm transferleri listeler.</summary>
    public async Task<IReadOnlyList<StockTransferResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _transferRepository.GetAllAsync(cancellationToken);
        return list.Select(StockTransferMapper.ToResponse).ToList();
    }

    /// <summary>Yeni transfer oluşturur (Pending).</summary>
    public async Task<StockTransferResponse> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var entity = StockTransferMapper.ToEntity(request);
        await _transferRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return StockTransferMapper.ToResponse(entity);
    }

    /// <summary>Transferi başlatır (InTransit).</summary>
    public async Task StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer bulunamadı: {id}");

        if (transfer.Status != StockTransferStatus.Pending)
            throw new InvalidOperationException("Sadece Pending transferler başlatılabilir.");

        foreach (var item in transfer.Items)
        {
            await ApplyStockChangeAsync(
                item.ProductId, transfer.FromWarehouseId, transfer.UserId,
                TransactionType.TransferOut, item.Quantity, transfer.Id, cancellationToken);
        }

        transfer.Status = StockTransferStatus.InTransit;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Transferi tamamlar (Completed).</summary>
    public async Task CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer bulunamadı: {id}");

        if (transfer.Status != StockTransferStatus.InTransit)
            throw new InvalidOperationException("Sadece InTransit transferler tamamlanabilir.");

        foreach (var item in transfer.Items)
        {
            await ApplyStockChangeAsync(
                item.ProductId, transfer.ToWarehouseId, transfer.UserId,
                TransactionType.TransferIn, item.Quantity, transfer.Id, cancellationToken);
        }

        transfer.Status = StockTransferStatus.Completed;
        transfer.CompletionDate = DateTime.UtcNow;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Transferi iptal eder; yoldaki stok varsa kaynak depoya iade edilir.</summary>
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
                    item.ProductId, transfer.FromWarehouseId, transfer.UserId,
                    TransactionType.TransferIn, item.Quantity, transfer.Id, cancellationToken);
            }
        }

        transfer.Status = StockTransferStatus.Cancelled;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Transfer kalemi için stok ve transaction kaydı uygular.</summary>
    private async Task ApplyStockChangeAsync(
        Guid productId, Guid warehouseId, Guid userId, TransactionType type,
        int quantity, Guid transferId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        if (inventory is null)
        {
            inventory = new Inventory
            {
                Id = Guid.NewGuid(),
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
