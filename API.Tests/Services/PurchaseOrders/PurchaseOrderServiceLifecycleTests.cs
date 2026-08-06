using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.PurchaseOrders;

/// <summary>
/// PurchaseOrderService Approve / Receive / Cancel
/// (durum geçişleri + Inventory In + kısmi kabul + concurrency).
/// </summary>
public class PurchaseOrderServiceLifecycleTests
{
    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepository = new();
    private readonly Mock<IStockTransactionRepository> _transactionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly PurchaseOrderService _sut;

    public PurchaseOrderServiceLifecycleTests()
    {
        _sut = PurchaseOrderServiceTestHelper.CreateSut(
            _purchaseOrderRepository,
            _transactionRepository,
            _inventoryRepository,
            _supplierRepository,
            _warehouseRepository,
            _productRepository,
            _unitOfWork,
            _currentUser);
    }

    /// <summary>Approve: sipariş yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task ApproveAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        var act = async () => await _sut.ApproveAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sipariş bulunamadı: {id}");
    }

    /// <summary>Approve: Pending değil → InvalidOperationException.</summary>
    [Theory]
    [InlineData(PurchaseOrderStatus.Approved)]
    [InlineData(PurchaseOrderStatus.Received)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public async Task ApproveAsync_WhenNotPending_ThrowsInvalidOperationException(PurchaseOrderStatus status)
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(status: status);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _sut.ApproveAsync(order.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece Pending siparişler onaylanabilir.");
    }

    /// <summary>Approve: Pending → Status = Approved; Inventory dokunulmaz.</summary>
    [Fact]
    public async Task ApproveAsync_WhenPending_SetsApprovedWithoutInventoryChange()
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(status: PurchaseOrderStatus.Pending);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ApproveAsync(order.Id);

        order.Status.Should().Be(PurchaseOrderStatus.Approved);
        _purchaseOrderRepository.Verify(r => r.Update(order), Times.Once);
        _inventoryRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Receive: sipariş yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        var act = async () => await _sut.ReceiveAsync(id, new Dictionary<Guid, int>());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sipariş bulunamadı: {id}");
    }

    /// <summary>Receive: Pending/Cancelled → InvalidOperationException.</summary>
    [Theory]
    [InlineData(PurchaseOrderStatus.Pending)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public async Task ReceiveAsync_WhenStatusNotReceivable_ThrowsInvalidOperationException(
        PurchaseOrderStatus status)
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(status: status);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var productId = order.Items.First().ProductId;
        var act = async () => await _sut.ReceiveAsync(
            order.Id, new Dictionary<Guid, int> { [productId] = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece Approved veya kısmen Received siparişlerde mal kabulü yapılabilir.");
    }

    /// <summary>Receive: fazla miktar → InvalidOperationException; Save yok.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenOverReceivedQuantity_ThrowsInvalidOperationException()
    {
        var productId = Guid.NewGuid();
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            status: PurchaseOrderStatus.Approved,
            productId: productId,
            quantity: 5,
            receivedQuantity: 2);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _sut.ReceiveAsync(
            order.Id, new Dictionary<Guid, int> { [productId] = 4 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün için fazla kabul miktarı: {productId}");
        order.Items.First().ReceivedQuantity.Should().Be(2);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Receive: Approved + tam kabul → Inventory artar; IN tx; Status = Received.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenFullReceive_IncreasesInventoryAndSetsReceived()
    {
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            companyId: companyId,
            warehouseId: warehouseId,
            userId: userId,
            productId: productId,
            quantity: 10,
            status: PurchaseOrderStatus.Approved);

        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var inventory = PurchaseOrderServiceTestHelper.CreateInventory(
            companyId, productId, warehouseId, quantity: 20);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        StockTransaction? addedTx = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => addedTx = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ReceiveAsync(order.Id, new Dictionary<Guid, int> { [productId] = 10 });

        inventory.Quantity.Should().Be(30);
        order.Items.First().ReceivedQuantity.Should().Be(10);
        order.Status.Should().Be(PurchaseOrderStatus.Received);
        addedTx.Should().NotBeNull();
        addedTx!.TransactionType.Should().Be(TransactionType.In);
        addedTx.Quantity.Should().Be(10);
        addedTx.WarehouseId.Should().Be(warehouseId);
        addedTx.PurchaseOrderId.Should().Be(order.Id);
        addedTx.UserId.Should().Be(userId);
        _purchaseOrderRepository.Verify(r => r.Update(order), Times.Once);
    }

    /// <summary>Receive: kısmi kabul → Status Approved kalır; ReceivedQuantity artar.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenPartialReceive_KeepsApprovedAndUpdatesReceivedQuantity()
    {
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            companyId: companyId,
            warehouseId: warehouseId,
            productId: productId,
            quantity: 10,
            status: PurchaseOrderStatus.Approved);

        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var inventory = PurchaseOrderServiceTestHelper.CreateInventory(
            companyId, productId, warehouseId, quantity: 0);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ReceiveAsync(order.Id, new Dictionary<Guid, int> { [productId] = 3 });

        inventory.Quantity.Should().Be(3);
        order.Items.First().ReceivedQuantity.Should().Be(3);
        order.Status.Should().Be(PurchaseOrderStatus.Approved);
    }

    /// <summary>Receive: Inventory yoksa oluşturulur; Quantity = kabul miktarı.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenInventoryMissing_CreatesInventoryAndAppliesIn()
    {
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            companyId: companyId,
            warehouseId: warehouseId,
            productId: productId,
            quantity: 6,
            status: PurchaseOrderStatus.Approved);

        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        Inventory? addedInventory = null;
        _inventoryRepository
            .Setup(r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .Callback<Inventory, CancellationToken>((inv, _) => addedInventory = inv)
            .Returns(Task.CompletedTask);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ReceiveAsync(order.Id, new Dictionary<Guid, int> { [productId] = 6 });

        addedInventory.Should().NotBeNull();
        addedInventory!.WarehouseId.Should().Be(warehouseId);
        addedInventory.ProductId.Should().Be(productId);
        addedInventory.CompanyId.Should().Be(companyId);
        addedInventory.Quantity.Should().Be(6);
        order.Status.Should().Be(PurchaseOrderStatus.Received);
    }

    /// <summary>Receive: miktar ≤ 0 veya ürün yok → kalem atlanır; Status değişmez.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenIncomingNotPositive_SkipsItemWithoutInventoryChange()
    {
        var productId = Guid.NewGuid();
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            productId: productId,
            quantity: 5,
            status: PurchaseOrderStatus.Approved);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ReceiveAsync(order.Id, new Dictionary<Guid, int> { [productId] = 0 });

        order.Items.First().ReceivedQuantity.Should().Be(0);
        order.Status.Should().Be(PurchaseOrderStatus.Approved);
        _inventoryRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Receive: SaveChanges concurrency → ConflictException.</summary>
    [Fact]
    public async Task ReceiveAsync_WhenSaveChangesConcurrencyConflict_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            companyId: companyId,
            warehouseId: warehouseId,
            productId: productId,
            quantity: 5,
            status: PurchaseOrderStatus.Approved);

        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var inventory = PurchaseOrderServiceTestHelper.CreateInventory(
            companyId, productId, warehouseId, quantity: 10);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.ReceiveAsync(
            order.Id, new Dictionary<Guid, int> { [productId] = 2 });

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Cancel: sipariş yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task CancelAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        var act = async () => await _sut.CancelAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sipariş bulunamadı: {id}");
    }

    /// <summary>Cancel: Received/Cancelled → InvalidOperationException.</summary>
    [Theory]
    [InlineData(PurchaseOrderStatus.Received)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public async Task CancelAsync_WhenReceivedOrCancelled_ThrowsInvalidOperationException(
        PurchaseOrderStatus status)
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(status: status);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _sut.CancelAsync(order.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Teslim alınmış veya iptal edilmiş sipariş iptal edilemez.");
    }

    /// <summary>Cancel: kısmi mal kabulü → InvalidOperationException.</summary>
    [Fact]
    public async Task CancelAsync_WhenPartialReceiveExists_ThrowsInvalidOperationException()
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            status: PurchaseOrderStatus.Approved,
            receivedQuantity: 2,
            quantity: 10);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _sut.CancelAsync(order.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Kısmi mal kabulü yapılmış sipariş iptal edilemez.");
        order.Status.Should().Be(PurchaseOrderStatus.Approved);
    }

    /// <summary>Cancel: Pending → Status = Cancelled; stok dokunulmaz.</summary>
    [Fact]
    public async Task CancelAsync_WhenPending_SetsCancelledWithoutStockChange()
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(status: PurchaseOrderStatus.Pending);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CancelAsync(order.Id);

        order.Status.Should().Be(PurchaseOrderStatus.Cancelled);
        _inventoryRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _purchaseOrderRepository.Verify(r => r.Update(order), Times.Once);
    }

    /// <summary>Cancel: Approved (kabul yok) → Status = Cancelled.</summary>
    [Fact]
    public async Task CancelAsync_WhenApprovedWithoutReceive_SetsCancelled()
    {
        var order = PurchaseOrderServiceTestHelper.CreateEntity(
            status: PurchaseOrderStatus.Approved, receivedQuantity: 0);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CancelAsync(order.Id);

        order.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }
}
