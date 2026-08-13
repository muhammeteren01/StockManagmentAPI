using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Integration.Sysmond.Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>SysmondSyncService CreateStock / UpdateStock birim testleri.</summary>
public class SysmondSyncServiceStockCommandTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondStockCommandService> _stockCommand = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Integration.Sysmond.Service.Services.SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
    private static readonly Guid MeasureUnitId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SysmondSyncServiceStockCommandTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork,
            stockCommand: _stockCommand);

        _companyRepository
            .Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(CompanyId));
    }

    [Fact]
    public async Task CreateStockAsync_WhenValid_CallsSysmondAndPersistsProduct()
    {
        var remoteStockId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var request = ValidCreateRequest();

        _stockCommand
            .Setup(c => c.CreateStockAsync(
                AccessToken,
                It.Is<SysmondStockCreateDto>(d =>
                    d.CompanyId == CompanyId &&
                    d.Name == "Ürün A" &&
                    d.Code == "SKU-A"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteStockId);

        Product? captured = null;
        _productRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateStockAsync(CompanyId, AccessToken, request, CancellationToken.None);

        result.Name.Should().Be("Ürün A");
        result.Sku.Should().Be("SKU-A");
        captured.Should().NotBeNull();
        captured!.ExternalSysmondId.Should().Be(remoteStockId);
        captured.CompanyId.Should().Be(CompanyId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStockAsync_WhenNameAndCodeMissing_ThrowsValidationException()
    {
        var request = new SysmondCreateStockRequest
        {
            MeasureUnitId = MeasureUnitId,
            Price = CreatePrice(10, 5)
        };

        var act = async () => await _sut.CreateStockAsync(CompanyId, AccessToken, request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("name");
        _stockCommand.Verify(
            c => c.CreateStockAsync(It.IsAny<string>(), It.IsAny<SysmondStockCreateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStockAsync_WhenProductExists_UpdatesRemoteAndLocal()
    {
        var remoteStockId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = remoteStockId,
            Name = "Eski",
            Sku = "OLD",
            Description = "d",
            Type = ProductType.Goods,
            Status = ProductStatus.Active,
            MeasureUnitId = MeasureUnitId,
            CreatedAt = DateTime.UtcNow
        };

        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteStockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new SysmondUpdateStockRequest
        {
            Name = "Yeni Ürün",
            Code = "NEW-SKU",
            Type = 10,
            IsActive = true,
            MeasureUnitId = MeasureUnitId
        };

        var result = await _sut.UpdateStockAsync(
            CompanyId, AccessToken, remoteStockId, request, CancellationToken.None);

        result.Name.Should().Be("Yeni Ürün");
        result.Sku.Should().Be("NEW-SKU");
        product.Name.Should().Be("Yeni Ürün");
        _stockCommand.Verify(
            c => c.UpdateStockAsync(
                AccessToken,
                It.Is<SysmondStockUpdateDto>(d => d.Id == remoteStockId && d.Name == "Yeni Ürün"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _productRepository.Verify(r => r.Update(product), Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        var remoteStockId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteStockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.UpdateStockAsync(
            CompanyId,
            AccessToken,
            remoteStockId,
            new SysmondUpdateStockRequest { Name = "X", Type = 10 },
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _stockCommand.Verify(
            c => c.UpdateStockAsync(It.IsAny<string>(), It.IsAny<SysmondStockUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static SysmondCreateStockRequest ValidCreateRequest() =>
        new()
        {
            Name = "Ürün A",
            Code = "SKU-A",
            Type = 10,
            MeasureUnitId = MeasureUnitId,
            Price = CreatePrice(100, 80)
        };

    private static SysmondDefaultStockPriceCreateDto CreatePrice(double sale, double purchase) =>
        new()
        {
            SaleUnitPrice = sale,
            PurchaseUnitPrice = purchase,
            SaleCurrencyId = 949,
            PurchaseCurrencyId = 949,
            MeasureUnitId = MeasureUnitId
        };
}
