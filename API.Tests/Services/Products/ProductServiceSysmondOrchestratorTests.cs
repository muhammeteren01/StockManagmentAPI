using Core.Abstractions;
using Core.DTOs.Products;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using FluentAssertions;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Service.Services;

namespace API.Tests.Services.Products;

/// <summary>Sysmond Enabled iken ProductService write → orchestrator delegasyonu.</summary>
public class ProductServiceSysmondOrchestratorTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ISysmondProductOrchestrator> _orchestrator = new();

    private ProductService CreateSut(bool enabled) =>
        new(
            _repository.Object,
            _categoryRepository.Object,
            _supplierRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            new Core.Validations.Products.CreateProductRequestValidator(),
            new Core.Validations.Products.UpdateProductRequestValidator(),
            _orchestrator.Object,
            Options.Create(new SysmondOptions { Enabled = enabled }));

    [Fact]
    public async Task Create_WhenSysmondEnabled_DelegatesToOrchestrator()
    {
        var companyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository, companyId, categoryId, supplierId);

        var request = ProductServiceTestHelper.ValidCreate(
            companyId: companyId, categoryId: categoryId, supplierId: supplierId);
        request.MeasureUnitId = Guid.NewGuid();

        var expected = new ProductResponse
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = request.Sku,
            Name = request.Name
        };
        _orchestrator
            .Setup(o => o.CreateAsync(companyId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut(enabled: true).CreateAsync(request, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        _orchestrator.Verify(o => o.CreateAsync(companyId, request, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<Core.Entities.Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenSysmondDisabled_WritesLocally()
    {
        var companyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        ProductServiceTestHelper.SetupSameCompanyReferences(
            _categoryRepository, _supplierRepository, _repository, companyId, categoryId, supplierId);

        var request = ProductServiceTestHelper.ValidCreate(
            companyId: companyId, categoryId: categoryId, supplierId: supplierId);

        _repository
            .Setup(r => r.AddAsync(It.IsAny<Core.Entities.Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut(enabled: false).CreateAsync(request, CancellationToken.None);

        result.Sku.Should().Be(request.Sku);
        _orchestrator.Verify(
            o => o.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(r => r.AddAsync(It.IsAny<Core.Entities.Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenSysmondEnabled_DelegatesToOrchestrator()
    {
        var id = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        ProductServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var entity = ProductServiceTestHelper.CreateEntity(id: id, companyId: companyId);
        entity.ExternalSysmondId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _orchestrator.Setup(o => o.DeleteAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await CreateSut(enabled: true).DeleteAsync(id, CancellationToken.None);

        _orchestrator.Verify(o => o.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.Remove(It.IsAny<Core.Entities.Product>()), Times.Never);
    }
}
