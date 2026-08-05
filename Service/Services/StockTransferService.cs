using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Depolar arası transfer oluşturma ve durum geçişlerini yönetir.</summary>
public class StockTransferService : GenericService<StockTransfer>, IStockTransferService
{
    private readonly IStockTransferRepository _transferRepository;
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public StockTransferService(
        IStockTransferRepository repository,
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _transferRepository = repository;
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
    }

    public override async Task<StockTransfer> CreateAsync(StockTransfer entity, CancellationToken cancellationToken = default)
    {
        if (entity.FromWarehouseId == entity.ToWarehouseId)
            throw new InvalidOperationException("Kaynak ve hedef depo aynı olamaz.");

        if (entity.Items is null || entity.Items.Count == 0)
            throw new InvalidOperationException("Transfer en az bir kalem içermelidir.");

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        foreach (var item in entity.Items)
        {
            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();

            if (item.Quantity <= 0)
                throw new InvalidOperationException("Transfer kalem quantity pozitif olmalıdır.");
        }

        entity.Status = StockTransferStatus.Pending;
        if (entity.TransferDate == default)
            entity.TransferDate = DateTime.UtcNow;

        return await base.CreateAsync(entity, cancellationToken);
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
                item.ProductId,
                transfer.FromWarehouseId,
                transfer.UserId,
                TransactionType.TransferOut,
                item.Quantity,
                transfer.Id,
                cancellationToken);
        }

        transfer.Status = StockTransferStatus.InTransit;
        _transferRepository.Update(transfer);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
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
                item.ProductId,
                transfer.ToWarehouseId,
                transfer.UserId,
                TransactionType.TransferIn,
                item.Quantity,
                transfer.Id,
                cancellationToken);
        }

        transfer.Status = StockTransferStatus.Completed;
        transfer.CompletionDate = DateTime.UtcNow;
        _transferRepository.Update(transfer);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Transfer bulunamadı: {id}");

        if (transfer.Status is StockTransferStatus.Completed or StockTransferStatus.Cancelled)
            throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş transfer iptal edilemez.");

        // Yolda olan stok kaynak depoya geri alınır
        if (transfer.Status == StockTransferStatus.InTransit)
        {
            foreach (var item in transfer.Items)
            {
                await ApplyStockChangeAsync(
                    item.ProductId,
                    transfer.FromWarehouseId,
                    transfer.UserId,
                    TransactionType.TransferIn,
                    item.Quantity,
                    transfer.Id,
                    cancellationToken);
            }
        }

        transfer.Status = StockTransferStatus.Cancelled;
        _transferRepository.Update(transfer);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyStockChangeAsync(
        Guid productId,
        Guid warehouseId,
        Guid userId,
        TransactionType type,
        int quantity,
        Guid transferId,
        CancellationToken cancellationToken)
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
