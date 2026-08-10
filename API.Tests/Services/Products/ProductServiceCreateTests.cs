using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Products;

/// <summary>ProductService.CreateAsync birim testleri (gerçek validator + TenantGuard + referans kontrolleri).</summary>
public class ProductServiceCreateTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly ProductService _sut;

    public ProductServiceCreateTests()
    {
        _sut = ProductServiceTestHelper.CreateSut(
            _repository, _categoryRepository, _supplierRepository, _unitOfWork, _currentUser);
    }

    /// <summary>Create: Name boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = ProductServiceTestHelper.ValidCreate(name: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: Sku boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSkuEmpty_ThrowsValidationException()
    {
        var request = ProductServiceTestHelper.ValidCreate(sku: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Sku));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: UnitPrice negatif → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenUnitPriceNegative_ThrowsValidationException()
    {
        var request = ProductServiceTestHelper.ValidCreate(unitPrice: -1m);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.UnitPrice));
    }

    /// <summary>Create: SuperAdmin, CompanyId yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        ProductServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = ProductServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin, token'da şirket yok → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminMissingCompanyInToken_ThrowsForbiddenException()
    {
        _currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        var request = ProductServiceTestHelper.ValidCreate();

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }

    /// <summary>Create: CategoryId verilmiş ama kategori yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCategoryNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(request.CategoryId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Kategori bulunamadı: {request.CategoryId}");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CategoryId/SupplierId null → referans kontrolü atlanır, kayıt oluşur.</summary>
    [Fact]
    public async Task CreateAsync_WhenCategoryAndSupplierNull_SucceedsWithoutReferenceChecks()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId);
        request.CategoryId = null;
        request.SupplierId = null;
        _repository
            .Setup(r => r.GetBySkuAsync(companyId, request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        Product? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.CategoryId.Should().BeNull();
        added.SupplierId.Should().BeNull();
        result.CategoryId.Should().BeNull();
        _categoryRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _supplierRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Create: kategori farklı şirkete ait → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCategoryOtherCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(request.CategoryId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductServiceTestHelper.CreateCategory(request.CategoryId!.Value, Guid.NewGuid()));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Kategori farklı bir şirkete ait.");
    }

    /// <summary>Create: tedarikçi yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSupplierNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(request.CategoryId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductServiceTestHelper.CreateCategory(request.CategoryId!.Value, companyId));
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Tedarikçi bulunamadı: {request.SupplierId}");
    }

    /// <summary>Create: tedarikçi farklı şirkete ait → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSupplierOtherCompany_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId);
        _categoryRepository
            .Setup(r => r.GetByIdAsync(request.CategoryId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductServiceTestHelper.CreateCategory(request.CategoryId!.Value, companyId));
        _supplierRepository
            .Setup(r => r.GetByIdAsync(request.SupplierId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductServiceTestHelper.CreateSupplier(request.SupplierId!.Value, Guid.NewGuid()));
        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tedarikçi farklı bir şirkete ait.");
    }

    /// <summary>Create: aynı şirkette SKU kullanılıyor → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSkuAlreadyUsed_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId, sku: "DUP-SKU");
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository,
            companyId, request.CategoryId, request.SupplierId);
        _repository
            .Setup(r => r.GetBySkuAsync(companyId, request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductServiceTestHelper.CreateEntity(companyId: companyId, sku: request.Sku));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin — CompanyId token'dan; Add + Save + response.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminSuccessful_UsesTokenCompanyIdAndSaves()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = ProductServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = Guid.NewGuid(); // token öncelikli; request yok sayılır
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository,
            companyId, request.CategoryId, request.SupplierId);

        Product? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(companyId);
        added.CategoryId.Should().Be(request.CategoryId);
        added.SupplierId.Should().Be(request.SupplierId);
        added.Sku.Should().Be(request.Sku);
        added.Name.Should().Be(request.Name);
        result.CompanyId.Should().Be(companyId);
        result.Name.Should().Be(request.Name);
        result.Sku.Should().Be(request.Sku);

        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin — request CompanyId kullanılır.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesRequestCompanyId()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = ProductServiceTestHelper.ValidCreate(companyId: companyId);
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository,
            companyId, request.CategoryId, request.SupplierId);

        Product? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().Be(companyId);
        result.CompanyId.Should().Be(companyId);
    }
}
