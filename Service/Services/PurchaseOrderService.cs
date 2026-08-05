using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Satın alma siparişi oluşturma, onaylama ve mal kabulünü yönetir.</summary>
public class PurchaseOrderService : GenericService<PurchaseOrder>, IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public PurchaseOrderService(
        IPurchaseOrderRepository repository,
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        IValidator<PurchaseOrder> validator)
        : base(repository, unitOfWork, validator)
    {
        _purchaseOrderRepository = repository;
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>Belirli şirkete ait satın alma siparişlerini listeler.</summary>
    public Task<IReadOnlyList<PurchaseOrder>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _purchaseOrderRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    /// <summary>Yeni sipariş oluşturur; toplam tutarı kalemlerden hesaplar.</summary>
    public override async Task<PurchaseOrder> CreateAsync(PurchaseOrder entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        foreach (var item in entity.Items ?? [])
        {
            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();
        }

        entity.Status = PurchaseOrderStatus.Pending;
        entity.TotalAmount = entity.Items?.Sum(x => x.Quantity * x.UnitPrice) ?? 0;

        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity, cancellationToken);
    }

    /// <summary>Pending siparişi onaylar (Approved).</summary>
    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sipariş bulunamadı: {id}");

        if (order.Status != PurchaseOrderStatus.Pending)
            throw new InvalidOperationException("Sadece Pending siparişler onaylanabilir.");

        order.Status = PurchaseOrderStatus.Approved;
        _purchaseOrderRepository.Update(order);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Mal kabulü yapar; stok girişi (IN) oluşturur ve received miktarı günceller.</summary>
    public async Task ReceiveAsync(Guid id, IDictionary<Guid, int> receivedQuantities, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sipariş bulunamadı: {id}");

        if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.Received))
            throw new InvalidOperationException("Sadece Approved veya kısmen Received siparişlerde mal kabulü yapılabilir.");

        foreach (var item in order.Items)
        {
            if (!receivedQuantities.TryGetValue(item.ProductId, out var incoming) || incoming <= 0)
                continue;

            var remaining = item.Quantity - item.ReceivedQuantity;
            if (incoming > remaining)
                throw new InvalidOperationException($"Ürün için fazla kabul miktarı: {item.ProductId}");

            item.ReceivedQuantity += incoming;

            var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(item.ProductId, order.WarehouseId, cancellationToken);
            if (inventory is null)
            {
                inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    WarehouseId = order.WarehouseId,
                    Quantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory, cancellationToken);
            }

            inventory.Quantity += incoming;
            inventory.LastUpdated = DateTime.UtcNow;

            await _transactionRepository.AddAsync(new StockTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                WarehouseId = order.WarehouseId,
                UserId = order.UserId,
                PurchaseOrderId = order.Id,
                TransactionType = TransactionType.In,
                Quantity = incoming,
                TransactionDate = DateTime.UtcNow
            }, cancellationToken);
        }

        order.Status = order.Items.All(x => x.ReceivedQuantity >= x.Quantity)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.Approved;

        _purchaseOrderRepository.Update(order);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Siparişi iptal eder; kısmi kabul yapılmışsa izin vermez.</summary>
    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sipariş bulunamadı: {id}");

        if (order.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("Teslim alınmış veya iptal edilmiş sipariş iptal edilemez.");

        if (order.Items.Any(x => x.ReceivedQuantity > 0))
            throw new InvalidOperationException("Kısmi mal kabulü yapılmış sipariş iptal edilemez.");

        order.Status = PurchaseOrderStatus.Cancelled;
        _purchaseOrderRepository.Update(order);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
