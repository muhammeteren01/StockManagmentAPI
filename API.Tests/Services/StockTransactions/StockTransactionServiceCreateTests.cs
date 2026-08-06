using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.StockTransactions;

/// <summary>
/// StockTransactionService.CreateAsync birim testleri
/// (validator + aynı şirket + Inventory miktar + concurrency).
/// </summary>
public class StockTransactionServiceCreateTests
{
    private readonly Mock<IStockTransactionRepository> _transactionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly StockTransactionService _sut;

    public StockTransactionServiceCreateTests()
    {
        _sut = StockTransactionServiceTestHelper.CreateSut(
            _transactionRepository,
            _inventoryRepository,
            _productRepository,
            _warehouseRepository,
            _unitOfWork,
            _currentUser);
    }

    /// <summary>Create: Quantity ≤ 0 → ValidationException; repository çağrılmaz.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WhenQuantityNotPositive_ThrowsValidationException(int quantity)
    {
        var request = StockTransactionServiceTestHelper.ValidCreate(quantity: quantity);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Quantity));
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: ProductId boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenProductIdEmpty_ThrowsValidationException()
    {
        var request = StockTransactionServiceTestHelper.ValidCreate(productId: Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.ProductId));
    }

    /// <summary>Create: TransactionType enum dışı → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenTransactionTypeOutOfEnum_ThrowsValidationException()
    {
        var request = StockTransactionServiceTestHelper.ValidCreate();
        request.TransactionType = (TransactionType)999;

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.TransactionType));
    }

    /// <summary>Create: ürün yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate();
        _productRepository
            .Setup(r => r.GetByIdAsync(request.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün bulunamadı: {request.ProductId}");
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: depo yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenWarehouseNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate();
        _productRepository
            .Setup(r => r.GetByIdAsync(request.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransactionServiceTestHelper.CreateProduct(request.ProductId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Depo bulunamadı: {request.WarehouseId}");
    }

    /// <summary>Create: ürün ve depo farklı şirket → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenProductAndWarehouseDifferentCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate();
        _productRepository
            .Setup(r => r.GetByIdAsync(request.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransactionServiceTestHelper.CreateProduct(request.ProductId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransactionServiceTestHelper.CreateWarehouse(request.WarehouseId, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ürün ve depo aynı şirkete ait olmalıdır.");
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin başka şirketin ürününe erişemez → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var tokenCompanyId = Guid.NewGuid();
        var productCompanyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, tokenCompanyId);
        var request = StockTransactionServiceTestHelper.ValidCreate();
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            productCompanyId, request.ProductId, request.WarehouseId);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }

    /// <summary>Create: token'da UserId yok → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenUserIdMissing_ThrowsForbiddenException()
    {
        var companyId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(c => c.CompanyId).Returns(companyId);
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);
        _currentUser.SetupGet(c => c.Role).Returns(UserRole.CompanyAdmin);

        var request = StockTransactionServiceTestHelper.ValidCreate();
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Kullanıcı kimliği bulunamadı.");
    }

    /// <summary>Create: In — mevcut Inventory Quantity artar; Add + Save.</summary>
    [Fact]
    public async Task CreateAsync_WhenIn_IncreasesInventoryQuantityAndSaves()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId, userId);
        var request = StockTransactionServiceTestHelper.ValidCreate(
            transactionType: TransactionType.In, quantity: 15);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var inventory = StockTransactionServiceTestHelper.CreateInventory(
            companyId, request.ProductId, request.WarehouseId, quantity: 40);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        StockTransaction? added = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        inventory.Quantity.Should().Be(55);
        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(companyId);
        added.UserId.Should().Be(userId);
        added.TransactionType.Should().Be(TransactionType.In);
        added.Quantity.Should().Be(15);
        result.CompanyId.Should().Be(companyId);
        result.Quantity.Should().Be(15);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactionRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: Out — Inventory Quantity azalır.</summary>
    [Fact]
    public async Task CreateAsync_WhenOut_DecreasesInventoryQuantity()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupStaffCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate(
            transactionType: TransactionType.Out, quantity: 12);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var inventory = StockTransactionServiceTestHelper.CreateInventory(
            companyId, request.ProductId, request.WarehouseId, quantity: 30);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CreateAsync(request);

        inventory.Quantity.Should().Be(18);
    }

    /// <summary>Create: TransferIn — Quantity artar; TransferOut — azalır.</summary>
    [Theory]
    [InlineData(TransactionType.TransferIn, 50, 10, 60)]
    [InlineData(TransactionType.TransferOut, 50, 10, 40)]
    [InlineData(TransactionType.Adjustment, 50, 10, 40)]
    public async Task CreateAsync_WhenTransferOrAdjustment_AppliesCorrectDelta(
        TransactionType type, int startQty, int qty, int expectedQty)
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate(
            transactionType: type, quantity: qty);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var inventory = StockTransactionServiceTestHelper.CreateInventory(
            companyId, request.ProductId, request.WarehouseId, quantity: startQty);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.CreateAsync(request);

        inventory.Quantity.Should().Be(expectedQty);
    }

    /// <summary>Create: Out, stok yetersiz → InvalidOperationException; Save yok.</summary>
    [Fact]
    public async Task CreateAsync_WhenInsufficientStock_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate(
            transactionType: TransactionType.Out, quantity: 25);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var inventory = StockTransactionServiceTestHelper.CreateInventory(
            companyId, request.ProductId, request.WarehouseId, quantity: 10);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Yetersiz stok.");
        inventory.Quantity.Should().Be(10);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Create: Inventory yoksa sıfırdan oluşturulur; In sonrası Quantity = miktar.</summary>
    [Fact]
    public async Task CreateAsync_WhenInventoryMissing_CreatesInventoryAndAppliesIn()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate(
            transactionType: TransactionType.In, quantity: 7);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
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

        await _sut.CreateAsync(request);

        addedInventory.Should().NotBeNull();
        addedInventory!.CompanyId.Should().Be(companyId);
        addedInventory.ProductId.Should().Be(request.ProductId);
        addedInventory.WarehouseId.Should().Be(request.WarehouseId);
        addedInventory.Quantity.Should().Be(7);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: Inventory yokken Out → yetersiz stok (0 - qty).</summary>
    [Fact]
    public async Task CreateAsync_WhenInventoryMissingAndOut_ThrowsInsufficientStock()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate(
            transactionType: TransactionType.Out, quantity: 1);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);
        _inventoryRepository
            .Setup(r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Yetersiz stok.");
    }

    /// <summary>
    /// Create: SaveChanges ConflictException (rowversion / concurrent inventory) iletilir.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenSaveChangesConcurrencyConflict_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransactionServiceTestHelper.ValidCreate();
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var inventory = StockTransactionServiceTestHelper.CreateInventory(
            companyId, request.ProductId, request.WarehouseId, quantity: 100);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
    }

    /// <summary>Create: SuperAdmin — ürün şirketinden CompanyId; erişim serbest.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesProductCompanyId()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        StockTransactionServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser, userId);
        var request = StockTransactionServiceTestHelper.ValidCreate(quantity: 3);
        StockTransactionServiceTestHelper.SetupSameCompanyProductAndWarehouse(
            _productRepository, _warehouseRepository,
            companyId, request.ProductId, request.WarehouseId);

        var inventory = StockTransactionServiceTestHelper.CreateInventory(
            companyId, request.ProductId, request.WarehouseId, quantity: 0);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        StockTransaction? added = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().Be(companyId);
        added.UserId.Should().Be(userId);
        result.CompanyId.Should().Be(companyId);
        inventory.Quantity.Should().Be(3);
    }

    /// <summary>GetById: entity var → response; yok → null.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var entity = StockTransactionServiceTestHelper.CreateEntity(quantity: 9);
        _transactionRepository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Quantity.Should().Be(9);
    }

    /// <summary>GetById: bulunamadı → null.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _transactionRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransaction?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    /// <summary>GetAll: repository listesini map'ler.</summary>
    [Fact]
    public async Task GetAllAsync_WhenItemsExist_ReturnsMappedList()
    {
        var entities = new List<StockTransaction>
        {
            StockTransactionServiceTestHelper.CreateEntity(quantity: 1),
            StockTransactionServiceTestHelper.CreateEntity(quantity: 2)
        };
        _transactionRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(r => r.Quantity).Should().BeEquivalentTo([1, 2]);
    }

    /// <summary>GetByProductId: ürün id ile filtreler.</summary>
    [Fact]
    public async Task GetByProductIdAsync_WhenItemsExist_ReturnsMappedList()
    {
        var productId = Guid.NewGuid();
        var entities = new List<StockTransaction>
        {
            StockTransactionServiceTestHelper.CreateEntity(productId: productId, quantity: 4)
        };
        _transactionRepository
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetByProductIdAsync(productId);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productId);
    }

    /// <summary>GetByWarehouseId: depo id ile filtreler.</summary>
    [Fact]
    public async Task GetByWarehouseIdAsync_WhenItemsExist_ReturnsMappedList()
    {
        var warehouseId = Guid.NewGuid();
        var entities = new List<StockTransaction>
        {
            StockTransactionServiceTestHelper.CreateEntity(warehouseId: warehouseId, quantity: 6)
        };
        _transactionRepository
            .Setup(r => r.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetByWarehouseIdAsync(warehouseId);

        result.Should().HaveCount(1);
        result[0].WarehouseId.Should().Be(warehouseId);
    }
}
