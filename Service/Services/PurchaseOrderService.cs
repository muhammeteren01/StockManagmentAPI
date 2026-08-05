using Core.DTOs.PurchaseOrders;
using Core.Entities;
using Core.Enums;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Satın alma siparişi yönetir (DTO).</summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IStockTransactionRepository _transactionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePurchaseOrderRequest> _createValidator;

    public PurchaseOrderService(
        IPurchaseOrderRepository purchaseOrderRepository,
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreatePurchaseOrderRequest> createValidator)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    /// <summary>Id ile siparişi (kalemler dahil) getirir.</summary>
    public async Task<PurchaseOrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _purchaseOrderRepository.GetByIdWithItemsAsync(id, cancellationToken);
        return entity is null ? null : PurchaseOrderMapper.ToResponse(entity);
    }

    /// <summary>Tüm siparişleri listeler.</summary>
    public async Task<IReadOnlyList<PurchaseOrderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _purchaseOrderRepository.GetAllAsync(cancellationToken);
        return list.Select(PurchaseOrderMapper.ToResponse).ToList();
    }

    /// <summary>Şirkete ait siparişleri listeler.</summary>
    public async Task<IReadOnlyList<PurchaseOrderResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await _purchaseOrderRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(PurchaseOrderMapper.ToResponse).ToList();
    }

    /// <summary>Yeni sipariş oluşturur (Pending).</summary>
    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var entity = PurchaseOrderMapper.ToEntity(request);
        await _purchaseOrderRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PurchaseOrderMapper.ToResponse(entity);
    }

    /// <summary>Pending siparişi onaylar.</summary>
    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sipariş bulunamadı: {id}");

        if (order.Status != PurchaseOrderStatus.Pending)
            throw new InvalidOperationException("Sadece Pending siparişler onaylanabilir.");

        order.Status = PurchaseOrderStatus.Approved;
        _purchaseOrderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Mal kabulü yapar; stok girişi (IN) oluşturur.</summary>
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Siparişi iptal eder.</summary>
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
