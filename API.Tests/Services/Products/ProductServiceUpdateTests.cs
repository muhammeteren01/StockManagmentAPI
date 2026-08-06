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

/// <summary>ProductService.UpdateAsync / GetByCompanyIdAsync / DeleteAsync birim testleri.</summary>
public class ProductServiceUpdateTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly ProductService _sut;

    public ProductServiceUpdateTests()
    {
        _sut = ProductServiceTestHelper.CreateSut(
            _repository, _categoryRepository, _supplierRepository, _unitOfWork, _currentUser);
    }

    /// <summary>Update: Name boş → ValidationException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = ProductServiceTestHelper.ValidUpdate(name: "");

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Update: ürün yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = ProductServiceTestHelper.ValidUpdate();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product bulunamadı: {id}");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Update: başka üründe aynı SKU → InvalidOperationException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSkuUsedByAnotherProduct_ThrowsInvalidOperationException()
    {
        var entity = ProductServiceTestHelper.CreateEntity();
        var request = ProductServiceTestHelper.ValidUpdate(sku: "TAKEN");
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository,
            entity.CompanyId, request.CategoryId, request.SupplierId);
        _repository
            .Setup(r => r.GetBySkuAsync(entity.CompanyId, request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductServiceTestHelper.CreateEntity(
                id: Guid.NewGuid(), companyId: entity.CompanyId, sku: request.Sku));

        var act = async () => await _sut.UpdateAsync(entity.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Update: geçerli istek → Update + SaveChanges + response.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSuccessful_UpdatesSavesAndReturnsResponse()
    {
        var entity = ProductServiceTestHelper.CreateEntity();
        var request = ProductServiceTestHelper.ValidUpdate(
            name: "Yeni Ürün",
            sku: "NEW-SKU",
            unitPrice: 200m,
            sellingPrice: 250m,
            minStockLevel: 3);
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository,
            entity.CompanyId, request.CategoryId, request.SupplierId);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(entity.Id, request);

        entity.Name.Should().Be("Yeni Ürün");
        entity.Sku.Should().Be("NEW-SKU");
        entity.CategoryId.Should().Be(request.CategoryId);
        entity.SupplierId.Should().Be(request.SupplierId);
        entity.UnitPrice.Should().Be(200m);
        result.Name.Should().Be("Yeni Ürün");
        result.Sku.Should().Be("NEW-SKU");

        _repository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Update: aynı ürün kendi SKU'sunu tutarsa çakışma sayılmaz.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSkuBelongsToSameProduct_Succeeds()
    {
        var entity = ProductServiceTestHelper.CreateEntity(sku: "KEEP-SKU");
        var request = ProductServiceTestHelper.ValidUpdate(sku: "KEEP-SKU", name: "Aynı SKU");
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository,
            entity.CompanyId, request.CategoryId, request.SupplierId);
        _repository
            .Setup(r => r.GetBySkuAsync(entity.CompanyId, request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(entity.Id, request);

        result.Sku.Should().Be("KEEP-SKU");
        result.Name.Should().Be("Aynı SKU");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByCompanyId: başka şirket → ForbiddenException.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenOtherCompany_ThrowsForbiddenException()
    {
        var ownCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, ownCompanyId);

        var act = async () => await _sut.GetByCompanyIdAsync(otherCompanyId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
        _repository.Verify(
            r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>GetByCompanyId: SuperAdmin herhangi bir şirkete erişebilir.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenSuperAdmin_ReturnsList()
    {
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var list = new List<Product>
        {
            ProductServiceTestHelper.CreateEntity(companyId: companyId)
        };
        _repository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _sut.GetByCompanyIdAsync(companyId);

        result.Should().HaveCount(1);
        result[0].CompanyId.Should().Be(companyId);
    }

    /// <summary>Delete: ürün yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.DeleteAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product bulunamadı: {id}");
    }

    /// <summary>Delete: ürün var → Remove + SaveChanges.</summary>
    [Fact]
    public async Task DeleteAsync_WhenSuccessful_RemovesAndSaves()
    {
        var entity = ProductServiceTestHelper.CreateEntity();
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(entity.Id);

        _repository.Verify(r => r.Remove(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
