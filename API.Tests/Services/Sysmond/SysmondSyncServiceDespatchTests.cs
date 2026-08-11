using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentAssertions;
using Moq;

namespace API.Tests.Services.Sysmond;

/// <summary>SysmondSyncService.SyncDespatchesAsync birim smoke testleri.</summary>
public class SysmondSyncServiceDespatchTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondDespatchQueryService> _despatchQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IStockTransactionRepository> _txRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Service.Services.Sysmond.SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
    private static readonly Guid PeriodId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public SysmondSyncServiceDespatchTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork,
            _despatchQuery,
            _txRepository,
            _userRepository);

        _inventoryQuery
            .Setup(s => s.GetMyCompanyPeriodsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondCompanyPeriodDto>
            {
                new()
                {
                    Id = PeriodId,
                    CompanyId = CompanyId,
                    IsActive = true,
                    Name = "2026"
                }
            });

        _productRepository
            .Setup(r => r.GetBySkuAsync(CompanyId, "__SYSMOND_NO_STOCK__", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _warehouseRepository
            .Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Warehouse>());

        _txRepository
            .Setup(r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockTransaction>());
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenCompanyIdEmpty_ThrowsValidationException()
    {
        var act = async () => await _sut.SyncDespatchesAsync(Guid.Empty, AccessToken);
        await act.Should().ThrowAsync<Core.Validations.ValidationException>();
        _despatchQuery.Verify(
            s => s.GetDespatchesAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenIncomingItem_CreatesStockTransactionInAndIncrementsInventory()
    {
        var company = SysmondSyncServiceTestHelper.CreateCompany(CompanyId);
        var user = SysmondSyncServiceTestHelper.CreateUser(CompanyId);
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = stockExt,
            Sku = "SKU-1",
            Name = "Ürün",
            Description = "",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = whExt,
            Name = "Depo",
            Location = "A",
            IsActive = true
        };
        var despatch = SysmondSyncServiceTestHelper.CreateDespatch();
        var item = SysmondSyncServiceTestHelper.CreateDespatchItem(despatch.Id, stockExt, whExt, quantity: 7);

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _despatchQuery.Setup(s => s.GetDespatchesAsync(AccessToken, CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { despatch });
        _despatchQuery.Setup(s => s.GetDespatchItemsAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _txRepository.Setup(r => r.GetByExternalSysmondIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransaction?)null);
        _inventoryRepository.Setup(r => r.GetByProductAndWarehouseAsync(product.Id, warehouse.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        StockTransaction? added = null;
        _txRepository.Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);

        Inventory? addedInv = null;
        _inventoryRepository.Setup(r => r.AddAsync(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .Callback<Inventory, CancellationToken>((inv, _) => addedInv = inv)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.DespatchesFetched.Should().Be(1);
        result.ItemsFetched.Should().Be(1);
        result.Created.Should().Be(1);
        result.Failed.Should().Be(0);
        added.Should().NotBeNull();
        added!.TransactionType.Should().Be(TransactionType.In);
        added.Quantity.Should().Be(7);
        added.ExternalSysmondId.Should().Be(item.Id);
        added.ExternalSysmondDespatchId.Should().Be(despatch.Id);
        added.ExternalSysmondCompanyPeriodId.Should().Be(PeriodId);
        added.ReferenceNo.Should().Be("IRS-1");
        addedInv.Should().NotBeNull();
        addedInv!.Quantity.Should().Be(7);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenRemoteSubset_DeletesSamePeriodOrphanAndReversesInventory()
    {
        var company = SysmondSyncServiceTestHelper.CreateCompany(CompanyId);
        var user = SysmondSyncServiceTestHelper.CreateUser(CompanyId);
        var remoteDespatch = SysmondSyncServiceTestHelper.CreateDespatch(companyPeriodId: PeriodId);
        var remoteStockExt = Guid.NewGuid();
        var remoteWhExt = Guid.NewGuid();
        var remoteProduct = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = remoteStockExt,
            Sku = "SKU-R",
            Name = "R",
            Description = "",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var remoteWh = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = remoteWhExt,
            Name = "Depo",
            Location = "A",
            IsActive = true
        };
        var remoteItem = SysmondSyncServiceTestHelper.CreateDespatchItem(
            remoteDespatch.Id, remoteStockExt, remoteWhExt, quantity: 1);

        var orphanExt = Guid.NewGuid();
        var orphanProductId = Guid.NewGuid();
        var orphanWhId = Guid.NewGuid();
        var orphanProduct = new Product
        {
            Id = orphanProductId,
            CompanyId = CompanyId,
            Sku = "SKU-ORPHAN",
            Name = "Orphan",
            Description = "",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var orphanWh = new Warehouse
        {
            Id = orphanWhId,
            CompanyId = CompanyId,
            Name = "OrphanDepo",
            Location = "B",
            IsActive = true
        };
        var orphanTx = new StockTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ProductId = orphanProductId,
            WarehouseId = orphanWhId,
            UserId = user.Id,
            ExternalSysmondId = orphanExt,
            ExternalSysmondDespatchId = Guid.NewGuid(),
            ExternalSysmondCompanyPeriodId = PeriodId,
            TransactionType = TransactionType.In,
            Quantity = 3,
            TransactionDate = DateTime.UtcNow
        };
        var otherPeriodOrphan = new StockTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ProductId = orphanProductId,
            WarehouseId = orphanWhId,
            UserId = user.Id,
            ExternalSysmondId = Guid.NewGuid(),
            ExternalSysmondDespatchId = Guid.NewGuid(),
            ExternalSysmondCompanyPeriodId = Guid.NewGuid(),
            TransactionType = TransactionType.In,
            Quantity = 9,
            TransactionDate = DateTime.UtcNow
        };
        var orphanInventory = new Inventory
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ProductId = orphanProductId,
            WarehouseId = orphanWhId,
            Quantity = 10,
            LastUpdated = DateTime.UtcNow
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _despatchQuery.Setup(s => s.GetDespatchesAsync(AccessToken, CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { remoteDespatch });
        _despatchQuery.Setup(s => s.GetDespatchItemsAsync(AccessToken, remoteDespatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { remoteItem });
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(remoteStockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteProduct);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(remoteWhExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteWh);
        _txRepository.Setup(r => r.GetByExternalSysmondIdAsync(remoteItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransaction?)null);
        _inventoryRepository.Setup(r => r.GetByProductAndWarehouseAsync(remoteProduct.Id, remoteWh.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Inventory
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ProductId = remoteProduct.Id,
                WarehouseId = remoteWh.Id,
                Quantity = 0,
                LastUpdated = DateTime.UtcNow
            });
        _txRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { orphanTx, otherPeriodOrphan });
        _txRepository.Setup(r => r.GetByIdAsync(orphanTx.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphanTx);
        _productRepository.Setup(r => r.GetByIdAsync(orphanProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphanProduct);
        _warehouseRepository.Setup(r => r.GetByIdAsync(orphanWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphanWh);
        _inventoryRepository.Setup(r => r.GetByProductAndWarehouseAsync(orphanProductId, orphanWhId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphanInventory);

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.Deleted.Should().Be(1);
        orphanInventory.Quantity.Should().Be(7);
        _txRepository.Verify(r => r.Remove(orphanTx), Times.Once);
        _txRepository.Verify(r => r.Remove(otherPeriodOrphan), Times.Never);
    }
}
