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

namespace API.Tests.Services.StockTransfers;

/// <summary>
/// StockTransferService.CreateAsync / Get birim testleri
/// (validator + depo kuralları + şirket + concurrency).
/// </summary>
public class StockTransferServiceCreateTests
{
    private readonly Mock<IStockTransferRepository> _transferRepository = new();
    private readonly Mock<IStockTransactionRepository> _transactionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly StockTransferService _sut;

    public StockTransferServiceCreateTests()
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

    /// <summary>Create: kalem Quantity ≤ 0 → ValidationException; repository çağrılmaz.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WhenItemQuantityNotPositive_ThrowsValidationException(int quantity)
    {
        var request = StockTransferServiceTestHelper.ValidCreate(quantity: quantity);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Items[0].Quantity");
        _transferRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: FromWarehouseId boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenFromWarehouseIdEmpty_ThrowsValidationException()
    {
        var request = StockTransferServiceTestHelper.ValidCreate(fromWarehouseId: Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.FromWarehouseId));
    }

    /// <summary>Create: kaynak = hedef (validator) → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenFromEqualsToWarehouse_ThrowsValidationException()
    {
        var warehouseId = Guid.NewGuid();
        var request = StockTransferServiceTestHelper.ValidCreate(
            fromWarehouseId: warehouseId, toWarehouseId: warehouseId);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.ToWarehouseId));
        _warehouseRepository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: Items boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenItemsEmpty_ThrowsValidationException()
    {
        var request = StockTransferServiceTestHelper.ValidCreate();
        request.Items.Clear();

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Items));
    }

    /// <summary>Create: kaynak depo yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenFromWarehouseNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.FromWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Kaynak depo bulunamadı: {request.FromWarehouseId}");
        _transferRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: hedef depo yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenToWarehouseNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.FromWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.FromWarehouseId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.ToWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Hedef depo bulunamadı: {request.ToWarehouseId}");
    }

    /// <summary>Create: depolar farklı şirket → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenWarehousesDifferentCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.FromWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.FromWarehouseId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.ToWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.ToWarehouseId, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Kaynak ve hedef depolar aynı şirkete ait olmalıdır.");
        _transferRepository.Verify(
            r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin başka şirketin deposuna erişemez → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var tokenCompanyId = Guid.NewGuid();
        var warehouseCompanyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, tokenCompanyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        StockTransferServiceTestHelper.SetupSameCompanyWarehousesAndProduct(
            _warehouseRepository, _productRepository,
            warehouseCompanyId, request.FromWarehouseId, request.ToWarehouseId, request.Items[0].ProductId);

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

        var request = StockTransferServiceTestHelper.ValidCreate();
        StockTransferServiceTestHelper.SetupSameCompanyWarehousesAndProduct(
            _warehouseRepository, _productRepository,
            companyId, request.FromWarehouseId, request.ToWarehouseId, request.Items[0].ProductId);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Kullanıcı kimliği bulunamadı.");
    }

    /// <summary>Create: ürün yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.FromWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.FromWarehouseId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.ToWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.ToWarehouseId, companyId));
        _productRepository
            .Setup(r => r.GetByIdAsync(request.Items[0].ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün bulunamadı: {request.Items[0].ProductId}");
    }

    /// <summary>Create: ürün farklı şirket → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenProductDifferentCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.FromWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.FromWarehouseId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.ToWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateWarehouse(request.ToWarehouseId, companyId));
        _productRepository
            .Setup(r => r.GetByIdAsync(request.Items[0].ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StockTransferServiceTestHelper.CreateProduct(request.Items[0].ProductId, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün farklı bir şirkete ait: {request.Items[0].ProductId}");
    }

    /// <summary>Create: geçerli istek → Pending transfer; Add + Save; Inventory dokunulmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenValid_CreatesPendingTransferWithoutTouchingInventory()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupManagerCurrentUser(_currentUser, companyId, userId);
        var request = StockTransferServiceTestHelper.ValidCreate(quantity: 7);
        StockTransferServiceTestHelper.SetupSameCompanyWarehousesAndProduct(
            _warehouseRepository, _productRepository,
            companyId, request.FromWarehouseId, request.ToWarehouseId, request.Items[0].ProductId);

        StockTransfer? added = null;
        _transferRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransfer, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.Status.Should().Be(StockTransferStatus.Pending);
        added.CompanyId.Should().Be(companyId);
        added.UserId.Should().Be(userId);
        added.FromWarehouseId.Should().Be(request.FromWarehouseId);
        added.ToWarehouseId.Should().Be(request.ToWarehouseId);
        added.Items.Should().HaveCount(1);
        added.Items.First().Quantity.Should().Be(7);
        result.Status.Should().Be(StockTransferStatus.Pending);
        result.CompanyId.Should().Be(companyId);
        _inventoryRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin — depo şirketinden CompanyId; erişim serbest.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesWarehouseCompanyId()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser, userId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        StockTransferServiceTestHelper.SetupSameCompanyWarehousesAndProduct(
            _warehouseRepository, _productRepository,
            companyId, request.FromWarehouseId, request.ToWarehouseId, request.Items[0].ProductId);

        StockTransfer? added = null;
        _transferRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransfer, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().Be(companyId);
        added.UserId.Should().Be(userId);
        result.CompanyId.Should().Be(companyId);
    }

    /// <summary>Create: SaveChanges ConflictException iletilir.</summary>
    [Fact]
    public async Task CreateAsync_WhenSaveChangesConcurrencyConflict_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        StockTransferServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = StockTransferServiceTestHelper.ValidCreate();
        StockTransferServiceTestHelper.SetupSameCompanyWarehousesAndProduct(
            _warehouseRepository, _productRepository,
            companyId, request.FromWarehouseId, request.ToWarehouseId, request.Items[0].ProductId);

        _transferRepository
            .Setup(r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
    }

    /// <summary>GetById: entity var → response; GetByIdWithItemsAsync kullanılır.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var entity = StockTransferServiceTestHelper.CreateEntity(quantity: 9);
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Items.Should().HaveCount(1);
        result.Items[0].Quantity.Should().Be(9);
    }

    /// <summary>GetById: bulunamadı → null.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _transferRepository
            .Setup(r => r.GetByIdWithItemsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransfer?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    /// <summary>GetAll: repository listesini map'ler.</summary>
    [Fact]
    public async Task GetAllAsync_WhenItemsExist_ReturnsMappedList()
    {
        var entities = new List<StockTransfer>
        {
            StockTransferServiceTestHelper.CreateEntity(referenceNo: "A"),
            StockTransferServiceTestHelper.CreateEntity(referenceNo: "B")
        };
        _transferRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(r => r.ReferenceNo).Should().BeEquivalentTo(["A", "B"]);
    }
}
