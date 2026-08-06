using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.StockTransfers;

/// <summary>
/// StockTransferService Start / Complete / Cancel
/// (durum geçişleri + Inventory TransferOut/In + concurrency).
/// </summary>
public class StockTransferServiceLifecycleTests
{
    private readonly Mock<IStockTransferRepository> _transferRepository = new();
    private readonly Mock<IStockTransactionRepository> _transactionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly StockTransferService _sut;

    public StockTransferServiceLifecycleTests()
    {
        _sut = StockTransferServiceTestHelper.CreateSut(
            _transferRepository,
            _transactionRepository,
            _inventoryRepository,
            _warehouseRepository,
            _productRepository,
            _unitOfWork,
            _currentUser);
    }

    /// <summary>Start: transfer yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task StartAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransfer?)null);

        var act = async () => await _sut.StartAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Transfer bulunamadı: {id}");
    }

    /// <summary>Start: Pending değil → InvalidOperationException.</summary>
    [Theory]
    [InlineData(StockTransferStatus.InTransit)]
    [InlineData(StockTransferStatus.Completed)]
    [InlineData(StockTransferStatus.Cancelled)]
    public async Task StartAsync_WhenNotPending_ThrowsInvalidOperationException(StockTransferStatus status)
    {
        var transfer = StockTransferServiceTestHelper.CreateEntity(status: status);
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var act = async () => await _sut.StartAsync(transfer.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece Pending transferler başlatılabilir.");
    }

    /// <summary>Start: Pending → kaynak depodan TransferOut; Status = InTransit.</summary>
    [Fact]
    public async Task StartAsync_WhenPending_DecreasesFromWarehouseInventoryAndSetsInTransit()
    {
        var companyId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            companyId: companyId,
            fromWarehouseId: fromId,
            toWarehouseId: toId,
            productId: productId,
            quantity: 12,
            status: StockTransferStatus.Pending);

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var inventory = StockTransferServiceTestHelper.CreateInventory(
            companyId, productId, fromId, quantity: 50);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        StockTransaction? addedTx = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => addedTx = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.StartAsync(transfer.Id);

        inventory.Quantity.Should().Be(38);
        transfer.Status.Should().Be(StockTransferStatus.InTransit);
        addedTx.Should().NotBeNull();
        addedTx!.TransactionType.Should().Be(TransactionType.TransferOut);
        addedTx.WarehouseId.Should().Be(fromId);
        addedTx.Quantity.Should().Be(12);
        addedTx.TransferId.Should().Be(transfer.Id);
        _transferRepository.Verify(r => r.Update(transfer), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Start: yetersiz stok → InvalidOperationException; Save yok.</summary>
    [Fact]
    public async Task StartAsync_WhenInsufficientStock_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            companyId: companyId,
            fromWarehouseId: fromId,
            productId: productId,
            quantity: 20,
            status: StockTransferStatus.Pending);

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var inventory = StockTransferServiceTestHelper.CreateInventory(
            companyId, productId, fromId, quantity: 5);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var act = async () => await _sut.StartAsync(transfer.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Yetersiz stok.");
        inventory.Quantity.Should().Be(5);
        transfer.Status.Should().Be(StockTransferStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Start: Inventory yok → oluşturup yetersiz stok.</summary>
    [Fact]
    public async Task StartAsync_WhenInventoryMissing_ThrowsInsufficientStock()
    {
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            quantity: 1, status: StockTransferStatus.Pending);
        var productId = transfer.Items.First().ProductId;
        var fromId = transfer.FromWarehouseId;

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);
        _inventoryRepository
            .Setup(r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = async () => await _sut.StartAsync(transfer.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Yetersiz stok.");
    }

    /// <summary>Start: SaveChanges concurrency → ConflictException.</summary>
    [Fact]
    public async Task StartAsync_WhenSaveChangesConcurrencyConflict_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            companyId: companyId,
            fromWarehouseId: fromId,
            productId: productId,
            quantity: 5,
            status: StockTransferStatus.Pending);

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var inventory = StockTransferServiceTestHelper.CreateInventory(
            companyId, productId, fromId, quantity: 100);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.StartAsync(transfer.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Complete: InTransit değil → InvalidOperationException.</summary>
    [Theory]
    [InlineData(StockTransferStatus.Pending)]
    [InlineData(StockTransferStatus.Completed)]
    [InlineData(StockTransferStatus.Cancelled)]
    public async Task CompleteAsync_WhenNotInTransit_ThrowsInvalidOperationException(StockTransferStatus status)
    {
        var transfer = StockTransferServiceTestHelper.CreateEntity(status: status);
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var act = async () => await _sut.CompleteAsync(transfer.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece InTransit transferler tamamlanabilir.");
    }

    /// <summary>Complete: InTransit → hedef depoya TransferIn; Status = Completed.</summary>
    [Fact]
    public async Task CompleteAsync_WhenInTransit_IncreasesToWarehouseInventoryAndSetsCompleted()
    {
        var companyId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            companyId: companyId,
            fromWarehouseId: fromId,
            toWarehouseId: toId,
            productId: productId,
            quantity: 8,
            status: StockTransferStatus.InTransit);

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var toInventory = StockTransferServiceTestHelper.CreateInventory(
            companyId, productId, toId, quantity: 20);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, toId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toInventory);

        StockTransaction? addedTx = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => addedTx = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CompleteAsync(transfer.Id);

        toInventory.Quantity.Should().Be(28);
        transfer.Status.Should().Be(StockTransferStatus.Completed);
        transfer.CompletionDate.Should().NotBeNull();
        addedTx!.TransactionType.Should().Be(TransactionType.TransferIn);
        addedTx.WarehouseId.Should().Be(toId);
        _transferRepository.Verify(r => r.Update(transfer), Times.Once);
    }

    /// <summary>Complete: hedef Inventory yoksa oluşturulur; Quantity = miktar.</summary>
    [Fact]
    public async Task CompleteAsync_WhenToInventoryMissing_CreatesInventoryAndAppliesIn()
    {
        var companyId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            companyId: companyId,
            toWarehouseId: toId,
            productId: productId,
            quantity: 6,
            status: StockTransferStatus.InTransit);

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, toId, It.IsAny<CancellationToken>()))
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

        await _sut.CompleteAsync(transfer.Id);

        addedInventory.Should().NotBeNull();
        addedInventory!.WarehouseId.Should().Be(toId);
        addedInventory.ProductId.Should().Be(productId);
        addedInventory.Quantity.Should().Be(6);
        transfer.Status.Should().Be(StockTransferStatus.Completed);
    }

    /// <summary>Cancel: Completed → InvalidOperationException.</summary>
    [Theory]
    [InlineData(StockTransferStatus.Completed)]
    [InlineData(StockTransferStatus.Cancelled)]
    public async Task CancelAsync_WhenCompletedOrCancelled_ThrowsInvalidOperationException(
        StockTransferStatus status)
    {
        var transfer = StockTransferServiceTestHelper.CreateEntity(status: status);
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var act = async () => await _sut.CancelAsync(transfer.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tamamlanmış veya iptal edilmiş transfer iptal edilemez.");
    }

    /// <summary>Cancel: Pending → stok dokunulmaz; Status = Cancelled.</summary>
    [Fact]
    public async Task CancelAsync_WhenPending_SetsCancelledWithoutStockChange()
    {
        var transfer = StockTransferServiceTestHelper.CreateEntity(status: StockTransferStatus.Pending);
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CancelAsync(transfer.Id);

        transfer.Status.Should().Be(StockTransferStatus.Cancelled);
        _inventoryRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transferRepository.Verify(r => r.Update(transfer), Times.Once);
    }

    /// <summary>Cancel: InTransit → kaynak depoya TransferIn geri; Status = Cancelled.</summary>
    [Fact]
    public async Task CancelAsync_WhenInTransit_RestoresFromWarehouseInventory()
    {
        var companyId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var transfer = StockTransferServiceTestHelper.CreateEntity(
            companyId: companyId,
            fromWarehouseId: fromId,
            productId: productId,
            quantity: 10,
            status: StockTransferStatus.InTransit);

        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(transfer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var inventory = StockTransferServiceTestHelper.CreateInventory(
            companyId, productId, fromId, quantity: 40);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        StockTransaction? addedTx = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => addedTx = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CancelAsync(transfer.Id);

        inventory.Quantity.Should().Be(50);
        transfer.Status.Should().Be(StockTransferStatus.Cancelled);
        addedTx!.TransactionType.Should().Be(TransactionType.TransferIn);
        addedTx.WarehouseId.Should().Be(fromId);
        addedTx.Quantity.Should().Be(10);
    }
}
