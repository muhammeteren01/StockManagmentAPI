using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services.Sysmond;

namespace API.Tests.Services.Sysmond;

/// <summary>SysmondSyncService.SyncProductsAsync / SyncInventoriesAsync birim testleri.</summary>
public class SysmondSyncServiceTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";

    public SysmondSyncServiceTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork);

        _productRepository
            .Setup(r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());
        _inventoryRepository
            .Setup(r => r.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Inventory>());
        _inventoryRepository
            .Setup(r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Inventory>());
        _warehouseRepository
            .Setup(r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Warehouse>());
        _inventoryQuery
            .Setup(s => s.GetWarehouseStocksAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondWarehouseStockDto>());
        _inventoryQuery
            .Setup(s => s.GetStockBalancesByWarehouseAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondStockBalanceDto>());
    }

    /// <summary>Sync: companyId Empty → ValidationException; stock-query çağrılmaz.</summary>
    [Fact]
    public async Task SyncProductsAsync_WhenCompanyIdEmpty_ThrowsValidationException()
    {
        var act = async () => await _sut.SyncProductsAsync(Guid.Empty, AccessToken);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("companyId");
        _stockQuery.Verify(
            s => s.GetAllStocksAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Sync: accessToken boş → ValidationException; stock-query çağrılmaz.</summary>
    [Fact]
    public async Task SyncProductsAsync_WhenAccessTokenMissing_ThrowsValidationException()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");

        var act = async () => await _sut.SyncProductsAsync(companyId, "  ");

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Authorization");
        _stockQuery.Verify(
            s => s.GetAllStocksAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Sync: geçerli companyId + token → stock-query token ile filtrelenmiş çağrılır; ürün oluşturulur.</summary>
    [Fact]
    public async Task SyncProductsAsync_WhenCompanyIdAndTokenProvided_FetchesFilteredAndCreatesProduct()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var stock = SysmondSyncServiceTestHelper.CreateStock(companyId);
        _stockQuery
            .Setup(s => s.GetAllStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockDto> { stock });
        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(stock.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepository
            .Setup(r => r.GetBySkuAsync(companyId, stock.Code!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.SyncProductsAsync(companyId, AccessToken);

        result.Fetched.Should().Be(1);
        result.Created.Should().Be(1);
        result.Failed.Should().Be(0);
        result.Deleted.Should().Be(0);
        _stockQuery.Verify(
            s => s.GetAllStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()),
            Times.Once);
        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Remote subset: local'da ekstra ExternalSysmondId ürünü → Deleted++; inventory + product Remove.
    /// </summary>
    [Fact]
    public async Task SyncProductsAsync_WhenRemoteSubset_DeletesLocalExtraExternalSysmondProduct()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var keptExternalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var orphanExternalId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var stock = SysmondSyncServiceTestHelper.CreateStock(companyId, keptExternalId, "SKU-KEEP");

        var kept = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "SKU-KEEP",
            Name = "Keep",
            ExternalSysmondId = keptExternalId,
            CreatedAt = DateTime.UtcNow
        };
        var orphan = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "SKU-ORPHAN",
            Name = "Orphan",
            ExternalSysmondId = orphanExternalId,
            CreatedAt = DateTime.UtcNow
        };
        var orphanInventory = new Inventory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = orphan.Id,
            WarehouseId = Guid.NewGuid(),
            Quantity = 5,
            LastUpdated = DateTime.UtcNow
        };

        _stockQuery
            .Setup(s => s.GetAllStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockDto> { stock });
        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(keptExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kept);
        _productRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { kept, orphan });
        _productRepository
            .Setup(r => r.GetByIdAsync(orphan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphan);
        _inventoryRepository
            .Setup(r => r.GetByProductIdAsync(orphan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inventory> { orphanInventory });

        var result = await _sut.SyncProductsAsync(companyId, AccessToken);

        result.Updated.Should().Be(1);
        result.Deleted.Should().Be(1);
        result.FailedDeletes.Should().Be(0);
        _inventoryRepository.Verify(r => r.Remove(orphanInventory), Times.Once);
        _productRepository.Verify(r => r.Remove(orphan), Times.Once);
        _productRepository.Verify(r => r.Remove(kept), Times.Never);
    }

    /// <summary>
    /// ExternalSysmondId null local ürün remote'da yoksa bile silinmez (yalnızca Sysmond-synced set).
    /// </summary>
    [Fact]
    public async Task SyncProductsAsync_WhenLocalProductHasNoExternalSysmondId_DoesNotDelete()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var stock = SysmondSyncServiceTestHelper.CreateStock(companyId);
        var localOnly = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "LOCAL-ONLY",
            Name = "Local create",
            ExternalSysmondId = null,
            CreatedAt = DateTime.UtcNow
        };

        _stockQuery
            .Setup(s => s.GetAllStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockDto> { stock });
        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(stock.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepository
            .Setup(r => r.GetBySkuAsync(companyId, stock.Code!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { localOnly });

        var result = await _sut.SyncProductsAsync(companyId, AccessToken);

        result.Created.Should().Be(1);
        result.Deleted.Should().Be(0);
        result.FailedDeletes.Should().Be(0);
        _productRepository.Verify(r => r.Remove(It.IsAny<Product>()), Times.Never);
        _productRepository.Verify(r => r.GetByIdAsync(localOnly.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Inventory sync: warehouse + balance → Inventory Created; warehouse upsert.</summary>
    [Fact]
    public async Task SyncInventoriesAsync_WhenBalanceAndProductExist_CreatesInventory()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var remoteWhId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var stockId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var remoteWh = SysmondSyncServiceTestHelper.CreateWarehouseDto(companyId, remoteWhId);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "SKU-1",
            Name = "P",
            ExternalSysmondId = stockId,
            CreatedAt = DateTime.UtcNow
        };

        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _inventoryQuery
            .Setup(s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondWarehouseDto> { remoteWh });
        _warehouseRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);
        _inventoryQuery
            .Setup(s => s.GetStockBalancesByWarehouseAsync(AccessToken, remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockBalanceDto>
            {
                SysmondSyncServiceTestHelper.CreateBalance(stockId, remoteWhId, 7.4)
            });
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(
                product.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var result = await _sut.SyncInventoriesAsync(companyId, AccessToken);

        result.WarehousesCreated.Should().Be(1);
        result.Fetched.Should().Be(1);
        result.Created.Should().Be(1);
        result.SkippedProductNotFound.Should().Be(0);
        _inventoryRepository.Verify(
            r => r.AddAsync(
                It.Is<Inventory>(i => i.Quantity == 7 && i.ProductId == product.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Inventory sync: product ExternalSysmondId yoksa skip; Created=0.</summary>
    [Fact]
    public async Task SyncInventoriesAsync_WhenProductMissing_SkipsRow()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var remoteWhId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var remoteWh = SysmondSyncServiceTestHelper.CreateWarehouseDto(companyId, remoteWhId);

        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _inventoryQuery
            .Setup(s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondWarehouseDto> { remoteWh });
        _warehouseRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);
        _warehouseRepository
            .Setup(r => r.GetByIdAsync(remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);
        _inventoryQuery
            .Setup(s => s.GetStockBalancesByWarehouseAsync(AccessToken, remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockBalanceDto>
            {
                SysmondSyncServiceTestHelper.CreateBalance(stockId, remoteWhId, 3)
            });
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.SyncInventoriesAsync(companyId, AccessToken);

        result.SkippedProductNotFound.Should().Be(1);
        result.Created.Should().Be(0);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Inventory orphan: remote empty + Sysmond-linked local → Deleted.</summary>
    [Fact]
    public async Task SyncInventoriesAsync_WhenRemoteEmpty_DeletesSysmondLinkedInventory()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "S",
            Name = "P",
            ExternalSysmondId = stockExt,
            CreatedAt = DateTime.UtcNow
        };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ExternalSysmondId = whExt,
            Name = "W",
            Location = "L",
            IsActive = true
        };
        var orphanInv = new Inventory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 2,
            LastUpdated = DateTime.UtcNow
        };

        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _inventoryQuery
            .Setup(s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondWarehouseDto>());
        _productRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _warehouseRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { warehouse });
        _inventoryRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inventory> { orphanInv });
        _inventoryRepository
            .Setup(r => r.GetByIdAsync(orphanInv.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphanInv);

        var result = await _sut.SyncInventoriesAsync(companyId, AccessToken);

        result.Deleted.Should().Be(1);
        _inventoryRepository.Verify(r => r.Remove(orphanInv), Times.Once);
    }

    /// <summary>Local-only inventory (no ExternalSysmondId on product/warehouse) silinmez.</summary>
    [Fact]
    public async Task SyncInventoriesAsync_WhenLocalOnlyInventory_DoesNotDelete()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "LOCAL",
            Name = "L",
            ExternalSysmondId = null,
            CreatedAt = DateTime.UtcNow
        };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ExternalSysmondId = null,
            Name = "Local WH",
            Location = "L",
            IsActive = true
        };
        var localInv = new Inventory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 9,
            LastUpdated = DateTime.UtcNow
        };

        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _inventoryQuery
            .Setup(s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondWarehouseDto>());
        _productRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _warehouseRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { warehouse });
        _inventoryRepository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inventory> { localInv });

        var result = await _sut.SyncInventoriesAsync(companyId, AccessToken);

        result.Deleted.Should().Be(0);
        _inventoryRepository.Verify(r => r.Remove(It.IsAny<Inventory>()), Times.Never);
    }

    /// <summary>warehouse-stock 403 → Errors soft; balance sync devam.</summary>
    [Fact]
    public async Task SyncInventoriesAsync_WhenWarehouseStockFails_ContinuesWithBalances()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        var remoteWhId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var remoteWh = SysmondSyncServiceTestHelper.CreateWarehouseDto(companyId, remoteWhId);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = "S",
            Name = "P",
            ExternalSysmondId = stockId,
            CreatedAt = DateTime.UtcNow
        };
        var localWh = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ExternalSysmondId = remoteWhId,
            Name = "W",
            Location = "L",
            IsActive = true
        };

        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _inventoryQuery
            .Setup(s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondWarehouseDto> { remoteWh });
        _warehouseRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localWh);
        _inventoryQuery
            .Setup(s => s.GetWarehouseStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sysmond warehouse-stock başarısız (403): forbidden"));
        _inventoryQuery
            .Setup(s => s.GetStockBalancesByWarehouseAsync(AccessToken, remoteWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockBalanceDto>
            {
                SysmondSyncServiceTestHelper.CreateBalance(stockId, remoteWhId, 4)
            });
        _productRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _inventoryRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(product.Id, localWh.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var result = await _sut.SyncInventoriesAsync(companyId, AccessToken);

        result.Created.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("warehouse-stock", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>SyncAll: products sonra inventories çağrılır.</summary>
    [Fact]
    public async Task SyncAllAsync_RunsProductsThenInventories()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        _stockQuery
            .Setup(s => s.GetAllStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondStockDto>());
        _companyRepository
            .Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(companyId));
        _inventoryQuery
            .Setup(s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondWarehouseDto>());

        var result = await _sut.SyncAllAsync(companyId, AccessToken);

        result.Products.Should().NotBeNull();
        result.Inventories.Should().NotBeNull();
        _stockQuery.Verify(
            s => s.GetAllStocksAsync(AccessToken, companyId, It.IsAny<CancellationToken>()),
            Times.Once);
        _inventoryQuery.Verify(
            s => s.GetWarehousesAsync(AccessToken, companyId, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
