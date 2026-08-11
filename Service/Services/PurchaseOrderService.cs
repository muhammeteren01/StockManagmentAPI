using Core.Abstractions;
using Core.Authorization;
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
    private readonly ISupplierRepository _supplierRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreatePurchaseOrderRequest> _createValidator;

    public PurchaseOrderService(
        IPurchaseOrderRepository purchaseOrderRepository,
        IStockTransactionRepository transactionRepository,
        IInventoryRepository inventoryRepository,
        ISupplierRepository supplierRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreatePurchaseOrderRequest> createValidator)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _transactionRepository = transactionRepository;
        _inventoryRepository = inventoryRepository;
        _supplierRepository = supplierRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    public async Task<PurchaseOrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _purchaseOrderRepository.GetByIdWithItemsAsync(id, cancellationToken);
        return entity is null ? null : PurchaseOrderMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<PurchaseOrderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _purchaseOrderRepository.GetAllAsync(cancellationToken);
        return list.Select(PurchaseOrderMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<PurchaseOrderResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureCompanyAccess(_currentUser, companyId);
        var list = await _purchaseOrderRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(PurchaseOrderMapper.ToResponse).ToList();
    }

    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var userId = TenantGuard.RequireUserId(_currentUser);
        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);
        await EnsureSameCompanyReferencesAsync(companyId, request, cancellationToken);

        var entity = PurchaseOrderMapper.ToEntity(request, companyId, userId);
        await _purchaseOrderRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PurchaseOrderMapper.ToResponse(entity);
    }

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

    public async Task ReceiveAsync(Guid id, IDictionary<Guid, int> receivedQuantities, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sipariş bulunamadı: {id}");

        if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.Received))
            throw new InvalidOperationException("Sadece Approved veya kısmen Received siparişlerde mal kabulü yapılabilir.");

        if (order.DocumentType != PurchaseOrderDocumentType.PurchaseOrder)
            throw new InvalidOperationException("Mal kabul yalnızca satın alma siparişleri için geçerlidir.");

        if (order.WarehouseId is null || order.WarehouseId == Guid.Empty)
            throw new InvalidOperationException("Mal kabul için depo zorunludur.");

        var warehouseId = order.WarehouseId.Value;

        foreach (var item in order.Items)
        {
            if (!receivedQuantities.TryGetValue(item.ProductId, out var incoming) || incoming <= 0)
                continue;

            var remaining = item.Quantity - item.ReceivedQuantity;
            if (incoming > remaining)
                throw new InvalidOperationException($"Ürün için fazla kabul miktarı: {item.ProductId}");

            item.ReceivedQuantity += incoming;

            var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(item.ProductId, warehouseId, cancellationToken);
            if (inventory is null)
            {
                inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    CompanyId = order.CompanyId,
                    ProductId = item.ProductId,
                    WarehouseId = warehouseId,
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
                CompanyId = order.CompanyId,
                ProductId = item.ProductId,
                WarehouseId = warehouseId,
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

    private async Task EnsureSameCompanyReferencesAsync(
        Guid companyId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new InvalidOperationException($"Tedarikçi bulunamadı: {request.SupplierId}");
        if (supplier.CompanyId != companyId)
            throw new InvalidOperationException("Tedarikçi farklı bir şirkete ait.");

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new InvalidOperationException($"Depo bulunamadı: {request.WarehouseId}");
        if (warehouse.CompanyId != companyId)
            throw new InvalidOperationException("Depo farklı bir şirkete ait.");

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Ürün bulunamadı: {item.ProductId}");
            if (product.CompanyId != companyId)
                throw new InvalidOperationException($"Ürün farklı bir şirkete ait: {item.ProductId}");
        }
    }
}
