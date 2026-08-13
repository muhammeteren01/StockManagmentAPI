using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Integration.Sysmond.Core.Services;
using Core.UnitOfWork;
using FluentAssertions;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>SysmondSyncService.SyncDespatchesAsync → PurchaseOrder belge senkronu.</summary>
public class SysmondSyncServiceDespatchTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondDespatchQueryService> _despatchQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IPurchaseOrderRepository> _poRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Integration.Sysmond.Service.Services.SysmondSyncService _sut;

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
            _userRepository,
            _poRepository);

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

        _poRepository
            .Setup(r => r.GetSysmondDespatchesByCompanyPeriodAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PurchaseOrder>());

        _despatchQuery
            .Setup(s => s.GetDespatchDeliveryAddressAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysmondDespatchDeliveryAddressDto?)null);

        _despatchQuery
            .Setup(s => s.GetDespatchPartiesAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondDespatchPartyDto>());
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenCompanyIdEmpty_ThrowsValidationException()
    {
        var act = async () => await _sut.SyncDespatchesAsync(Guid.Empty, AccessToken);
        await act.Should().ThrowAsync<global::Core.Validations.ValidationException>();
        _despatchQuery.Verify(
            s => s.GetDespatchesAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenIncomingItem_CreatesPurchaseOrderAndItem()
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
        var despatch = SysmondSyncServiceTestHelper.CreateDespatch(companyPeriodId: PeriodId);
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
        _poRepository.Setup(r => r.GetByExternalSysmondIdWithItemsAsync(despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        PurchaseOrder? added = null;
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.DespatchesFetched.Should().Be(1);
        result.ItemsFetched.Should().Be(1);
        result.Created.Should().Be(1);
        result.Failed.Should().Be(0);
        added.Should().NotBeNull();
        added!.DocumentType.Should().Be(PurchaseOrderDocumentType.IncomingDespatch);
        added.Direction.Should().Be(DespatchDirection.Incoming);
        added.ExternalSysmondId.Should().Be(despatch.Id);
        added.ExternalSysmondCompanyPeriodId.Should().Be(PeriodId);
        added.Status.Should().Be(PurchaseOrderStatus.Saved);
        added.Items.Should().HaveCount(1);
        added.Items.First().ProductId.Should().Be(product.Id);
        added.Items.First().WarehouseId.Should().Be(warehouse.Id);
        added.Items.First().Quantity.Should().Be(7);
        added.TotalAmount.Should().Be(70);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenRemoteSubset_DeletesSamePeriodOrphanOrder()
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

        var orphanOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            DocumentType = PurchaseOrderDocumentType.IncomingDespatch,
            Direction = DespatchDirection.Incoming,
            UserId = user.Id,
            OrderNumber = "OLD-1",
            ExternalSysmondId = Guid.NewGuid(),
            ExternalSysmondCompanyPeriodId = PeriodId,
            Status = PurchaseOrderStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        var otherPeriodOrphan = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            DocumentType = PurchaseOrderDocumentType.OutgoingDespatch,
            Direction = DespatchDirection.Outgoing,
            UserId = user.Id,
            OrderNumber = "OTHER-1",
            ExternalSysmondId = Guid.NewGuid(),
            ExternalSysmondCompanyPeriodId = Guid.NewGuid(),
            Status = PurchaseOrderStatus.Draft,
            CreatedAt = DateTime.UtcNow
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
        _poRepository.Setup(r => r.GetByExternalSysmondIdWithItemsAsync(remoteDespatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);
        _poRepository.Setup(r => r.GetSysmondDespatchesByCompanyPeriodAsync(CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { orphanOrder });

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.Deleted.Should().Be(1);
        _poRepository.Verify(r => r.Remove(orphanOrder), Times.Once);
        _poRepository.Verify(r => r.Remove(otherPeriodOrphan), Times.Never);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenDeliveryAddress_WritesCompanyAddressAndJson()
    {
        var company = SysmondSyncServiceTestHelper.CreateCompany(CompanyId);
        var user = SysmondSyncServiceTestHelper.CreateUser(CompanyId);
        var companyAddressId = Guid.NewGuid();
        var deliveryAddressId = Guid.NewGuid();
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = stockExt,
            Sku = "SKU-A",
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
        var despatch = SysmondSyncServiceTestHelper.CreateDespatch(
            companyPeriodId: PeriodId,
            companyAddressId: companyAddressId,
            deliveryAddressId: deliveryAddressId);
        var item = SysmondSyncServiceTestHelper.CreateDespatchItem(despatch.Id, stockExt, whExt, quantity: 1);
        var address = new SysmondDespatchDeliveryAddressDto
        {
            Id = deliveryAddressId,
            DespatchId = despatch.Id,
            Street = "Atatürk Cad. No:1",
            CityOther = "Ankara",
            CountryId = 1,
            Contact = new SysmondContactInfoDto { FirstName = "Ali", LastName = "Veli" }
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _despatchQuery.Setup(s => s.GetDespatchesAsync(AccessToken, CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { despatch });
        _despatchQuery.Setup(s => s.GetDespatchItemsAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        _despatchQuery.Setup(s => s.GetDespatchDeliveryAddressAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _poRepository.Setup(r => r.GetByExternalSysmondIdWithItemsAsync(despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        PurchaseOrder? added = null;
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.Created.Should().Be(1);
        result.Failed.Should().Be(0);
        added.Should().NotBeNull();
        added!.ExternalSysmondCompanyAddressId.Should().Be(companyAddressId);
        added.DeliveryAddressJson.Should().NotBeNullOrWhiteSpace();
        added.DeliveryAddressJson.Should().Contain("Ankara");
        added.DeliveryAddressJson.Should().Contain("street");
        added.DeliveryAddressJson.Should().Contain(deliveryAddressId.ToString());
        _despatchQuery.Verify(
            s => s.GetDespatchDeliveryAddressAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenNoDeliveryAddressId_StillFetchesAddressEndpoint()
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
            Sku = "SKU-B",
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
        var despatch = SysmondSyncServiceTestHelper.CreateDespatch(companyPeriodId: PeriodId);
        var item = SysmondSyncServiceTestHelper.CreateDespatchItem(despatch.Id, stockExt, whExt, quantity: 1);
        var address = new SysmondDespatchDeliveryAddressDto
        {
            Id = Guid.NewGuid(),
            DespatchId = despatch.Id,
            Street = "No DeliveryAddressId still ok",
            CountryId = 1
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _despatchQuery.Setup(s => s.GetDespatchesAsync(AccessToken, CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { despatch });
        _despatchQuery.Setup(s => s.GetDespatchItemsAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        _despatchQuery.Setup(s => s.GetDespatchDeliveryAddressAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _poRepository.Setup(r => r.GetByExternalSysmondIdWithItemsAsync(despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        PurchaseOrder? added = null;
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        despatch.DeliveryAddressId.Should().BeNull();
        _despatchQuery.Verify(
            s => s.GetDespatchDeliveryAddressAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        added.Should().NotBeNull();
        added!.DeliveryAddressJson.Should().Contain("No DeliveryAddressId still ok");
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenNoDelivery_FallsBackToCompanyAddress()
    {
        var company = SysmondSyncServiceTestHelper.CreateCompany(CompanyId);
        var user = SysmondSyncServiceTestHelper.CreateUser(CompanyId);
        var companyAddressId = Guid.NewGuid();
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = stockExt,
            Sku = "SKU-C",
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
        var despatch = SysmondSyncServiceTestHelper.CreateDespatch(
            companyPeriodId: PeriodId,
            companyAddressId: companyAddressId);
        var item = SysmondSyncServiceTestHelper.CreateDespatchItem(despatch.Id, stockExt, whExt, quantity: 1);
        var firma = new SysmondCompanyAddressDto
        {
            Id = companyAddressId,
            CompanyId = CompanyId,
            Street = "Firma Cad. No:10",
            CityName = "Istanbul",
            CountryId = 1
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _despatchQuery.Setup(s => s.GetDespatchesAsync(AccessToken, CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { despatch });
        _despatchQuery.Setup(s => s.GetDespatchItemsAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        _despatchQuery.Setup(s => s.GetDespatchDeliveryAddressAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysmondDespatchDeliveryAddressDto?)null);
        _despatchQuery.Setup(s => s.GetCompanyAddressByIdAsync(AccessToken, companyAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firma);
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _poRepository.Setup(r => r.GetByExternalSysmondIdWithItemsAsync(despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        PurchaseOrder? added = null;
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.Failed.Should().Be(0);
        added.Should().NotBeNull();
        added!.ExternalSysmondCompanyAddressId.Should().Be(companyAddressId);
        added.DeliveryAddressJson.Should().Contain("Firma Cad. No:10");
        added.DeliveryAddressJson.Should().Contain("Istanbul");
        _despatchQuery.Verify(
            s => s.GetCompanyAddressByIdAsync(AccessToken, companyAddressId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncDespatchesAsync_WhenNoDelivery_UsesDespatchPartyCariAddress()
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
            Sku = "SKU-D",
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
        // companyAddressId yok — önceki senaryoda 6 belge böyleydi
        var despatch = SysmondSyncServiceTestHelper.CreateDespatch(companyPeriodId: PeriodId);
        var item = SysmondSyncServiceTestHelper.CreateDespatchItem(despatch.Id, stockExt, whExt, quantity: 1);
        var seller = new SysmondDespatchPartyDto
        {
            Id = Guid.NewGuid(),
            Type = 30, // SellerSupplier
            ActName = "Cari Firma A.Ş.",
            Street = "Cari Sok. No:5",
            CityOther = "Bursa",
            CountryId = 1
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _despatchQuery.Setup(s => s.GetDespatchesAsync(AccessToken, CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { despatch });
        _despatchQuery.Setup(s => s.GetDespatchItemsAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        _despatchQuery.Setup(s => s.GetDespatchDeliveryAddressAsync(AccessToken, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysmondDespatchDeliveryAddressDto?)null);
        _despatchQuery.Setup(s => s.GetDespatchPartiesAsync(AccessToken, CompanyId, despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { seller });
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _poRepository.Setup(r => r.GetByExternalSysmondIdWithItemsAsync(despatch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        PurchaseOrder? added = null;
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncDespatchesAsync(CompanyId, AccessToken);

        result.Failed.Should().Be(0);
        added!.DeliveryAddressJson.Should().Contain("Cari Sok. No:5");
        added.DeliveryAddressJson.Should().Contain("Bursa");
        added.DeliveryAddressJson.Should().Contain("Cari Firma");
        _despatchQuery.Verify(
            s => s.GetCompanyAddressByIdAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
