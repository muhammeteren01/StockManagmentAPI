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

namespace API.Tests.Services.PurchaseOrders;

/// <summary>
/// PurchaseOrderService.CreateAsync / Get birim testleri
/// (validator + tedarikçi/depo/ürün aynı şirket + TenantGuard).
/// </summary>
public class PurchaseOrderServiceCreateTests
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

    public PurchaseOrderServiceCreateTests()
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

    /// <summary>Create: kalem Quantity ≤ 0 → ValidationException; repository çağrılmaz.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WhenItemQuantityNotPositive_ThrowsValidationException(int quantity)
    {
        var request = PurchaseOrderServiceTestHelper.ValidCreate(quantity: quantity);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Items[0].Quantity");
        _purchaseOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: SupplierId boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSupplierIdEmpty_ThrowsValidationException()
    {
        var request = PurchaseOrderServiceTestHelper.ValidCreate(supplierId: Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.SupplierId));
    }

    /// <summary>Create: WarehouseId boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenWarehouseIdEmpty_ThrowsValidationException()
    {
        var request = PurchaseOrderServiceTestHelper.ValidCreate(warehouseId: Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.WarehouseId));
    }

    /// <summary>Create: OrderNumber boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenOrderNumberEmpty_ThrowsValidationException()
    {
        var request = PurchaseOrderServiceTestHelper.ValidCreate(orderNumber: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.OrderNumber));
    }

    /// <summary>Create: Items boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenItemsEmpty_ThrowsValidationException()
    {
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        request.Items.Clear();

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Items));
    }

    /// <summary>Create: UnitPrice negatif → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenUnitPriceNegative_ThrowsValidationException()
    {
        var request = PurchaseOrderServiceTestHelper.ValidCreate(unitPrice: -1m);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Items[0].UnitPrice");
    }

    /// <summary>Create: tedarikçi yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSupplierNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Tedarikçi bulunamadı: {request.SupplierId}");
        _purchaseOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: tedarikçi farklı şirket → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSupplierDifferentCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateSupplier(request.SupplierId, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tedarikçi farklı bir şirkete ait.");
        _purchaseOrderRepository.Verify(
            r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: depo yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenWarehouseNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateSupplier(request.SupplierId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Depo bulunamadı: {request.WarehouseId}");
    }

    /// <summary>Create: depo farklı şirket → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenWarehouseDifferentCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateSupplier(request.SupplierId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateWarehouse(request.WarehouseId, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Depo farklı bir şirkete ait.");
    }

    /// <summary>Create: ürün yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateSupplier(request.SupplierId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateWarehouse(request.WarehouseId, companyId));
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
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateSupplier(request.SupplierId, companyId));
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(request.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateWarehouse(request.WarehouseId, companyId));
        _productRepository
            .Setup(r => r.GetByIdAsync(request.Items[0].ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PurchaseOrderServiceTestHelper.CreateProduct(request.Items[0].ProductId, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün farklı bir şirkete ait: {request.Items[0].ProductId}");
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

        var request = PurchaseOrderServiceTestHelper.ValidCreate();

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Kullanıcı kimliği bulunamadı.");
    }

    /// <summary>Create: SuperAdmin CompanyId vermez → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        PurchaseOrderServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = PurchaseOrderServiceTestHelper.ValidCreate(companyId: null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
    }

    /// <summary>Create: geçerli istek → Pending sipariş; TotalAmount hesaplanır; Inventory dokunulmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenValid_CreatesPendingOrderWithoutTouchingInventory()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupManagerCurrentUser(_currentUser, companyId, userId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate(quantity: 4, unitPrice: 12.5m);
        PurchaseOrderServiceTestHelper.SetupSameCompanyReferences(
            _supplierRepository, _warehouseRepository, _productRepository,
            companyId, request.SupplierId, request.WarehouseId, request.Items[0].ProductId);

        PurchaseOrder? added = null;
        _purchaseOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.Status.Should().Be(PurchaseOrderStatus.Pending);
        added.CompanyId.Should().Be(companyId);
        added.UserId.Should().Be(userId);
        added.SupplierId.Should().Be(request.SupplierId);
        added.WarehouseId.Should().Be(request.WarehouseId);
        added.TotalAmount.Should().Be(50m);
        added.Items.Should().HaveCount(1);
        added.Items.First().ReceivedQuantity.Should().Be(0);
        result.Status.Should().Be(PurchaseOrderStatus.Pending);
        result.TotalAmount.Should().Be(50m);
        _inventoryRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin — request.CompanyId kullanılır.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesRequestCompanyId()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser, userId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate(companyId: companyId);
        PurchaseOrderServiceTestHelper.SetupSameCompanyReferences(
            _supplierRepository, _warehouseRepository, _productRepository,
            companyId, request.SupplierId, request.WarehouseId, request.Items[0].ProductId);

        PurchaseOrder? added = null;
        _purchaseOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
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
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = PurchaseOrderServiceTestHelper.ValidCreate();
        PurchaseOrderServiceTestHelper.SetupSameCompanyReferences(
            _supplierRepository, _warehouseRepository, _productRepository,
            companyId, request.SupplierId, request.WarehouseId, request.Items[0].ProductId);

        _purchaseOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
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
        var entity = PurchaseOrderServiceTestHelper.CreateEntity(quantity: 9, unitPrice: 10m);
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Items.Should().HaveCount(1);
        result.Items[0].Quantity.Should().Be(9);
        result.TotalAmount.Should().Be(90m);
    }

    /// <summary>GetById: bulunamadı → null.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _purchaseOrderRepository
            .Setup(r => r.GetByIdWithItemsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    /// <summary>GetAll: repository listesini map'ler.</summary>
    [Fact]
    public async Task GetAllAsync_WhenItemsExist_ReturnsMappedList()
    {
        var entities = new List<PurchaseOrder>
        {
            PurchaseOrderServiceTestHelper.CreateEntity(orderNumber: "A"),
            PurchaseOrderServiceTestHelper.CreateEntity(orderNumber: "B")
        };
        _purchaseOrderRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(r => r.OrderNumber).Should().BeEquivalentTo(["A", "B"]);
    }

    /// <summary>GetByCompanyId: erişim yok → ForbiddenException.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var tokenCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, tokenCompanyId);

        var act = async () => await _sut.GetByCompanyIdAsync(otherCompanyId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
        _purchaseOrderRepository.Verify(
            r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>GetByCompanyId: erişim var → mapped liste.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenAllowed_ReturnsMappedList()
    {
        var companyId = Guid.NewGuid();
        PurchaseOrderServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var entities = new List<PurchaseOrder>
        {
            PurchaseOrderServiceTestHelper.CreateEntity(companyId: companyId, orderNumber: "C1")
        };
        _purchaseOrderRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetByCompanyIdAsync(companyId);

        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("C1");
        result[0].CompanyId.Should().Be(companyId);
    }
}
