using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentAssertions;
using Moq;

namespace API.Tests.Services.Sysmond;

public class SysmondSyncServiceIncomingDespatchCreateTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondDespatchQueryService> _despatchQuery = new();
    private readonly Mock<ISysmondDespatchCommandService> _despatchCommand = new();
    private readonly Mock<ISysmondActQueryService> _actQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IPurchaseOrderRepository> _poRepository = new();
    private readonly Mock<IActRepository> _actRepository = new();
    private readonly Mock<IActAddressRepository> _actAddressRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Service.Services.Sysmond.SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
    private static readonly Guid PeriodId = Guid.Parse("e04ebee4-bdca-b9f1-ed45-3a22008d01a1");
    private static readonly Guid PaperDespatchTemplateId = Guid.Parse("dd82d747-cd6a-4e61-8a17-3a22008d01a9");

    public SysmondSyncServiceIncomingDespatchCreateTests()
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
            _poRepository,
            _despatchCommand,
            actQuery: _actQuery,
            actRepository: _actRepository,
            actAddressRepository: _actAddressRepository);

        _actQuery
            .Setup(q => q.GetCompanyDocNoTemplatesAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondCompanyDocNoTemplateDto>
            {
                new()
                {
                    Id = PaperDespatchTemplateId,
                    CompanyId = CompanyId,
                    Type = 50,
                    Name = "Irsaliye",
                    IsDefault = true
                }
            });

        _actQuery
            .Setup(q => q.GetDespatchScenariosByActIdAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondDespatchScenarioTypeMapDto>
            {
                new()
                {
                    Scenario = new SysmondIdNameIntDto { Id = 30, Name = "KAĞIT" },
                    Types =
                    [
                        new SysmondIdNameIntDto { Id = 10, Name = "SEVK" },
                        new SysmondIdNameIntDto { Id = 20, Name = "MATBUDAN" }
                    ]
                }
            });

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
    }

    [Fact]
    public async Task CreateIncomingDespatchAsync_CallsDraftItemSave_AndWritesLocalOrder()
    {
        var company = SysmondSyncServiceTestHelper.CreateCompany(CompanyId);
        company.TaxNumber = "52819614916";
        company.TaxOffice = "NİLÜFER VERGİ DAİRESİ MÜD.";
        var user = SysmondSyncServiceTestHelper.CreateUser(CompanyId);
        var actId = Guid.NewGuid();
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var measureUnitId = Guid.NewGuid();
        var despatchId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();

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

        var request = new SysmondCreateIncomingDespatchRequest
        {
            ActId = actId,
            ActName = "Cari A",
            ActVknTckn = "1234567890",
            DocNo = "IRS-NEW",
            Items =
            [
                new SysmondCreateIncomingDespatchItemRequest
                {
                    StockId = stockExt,
                    WarehouseId = whExt,
                    MeasureUnitId = measureUnitId,
                    Quantity = 3,
                    UnitPrice = 12.5,
                    VatPercent = 20,
                    Name = "Kalem"
                }
            ]
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _stockQuery
            .Setup(q => q.GetAllStocksAsync(AccessToken, CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockDto>
            {
                new()
                {
                    Id = stockExt,
                    CompanyId = CompanyId,
                    Code = "SKU-1",
                    Name = "Ürün",
                    Type = 10,
                    IsActive = true,
                    Prices =
                    [
                        new SysmondStockPriceDto
                        {
                            Id = Guid.NewGuid(),
                            StockId = stockExt,
                            StockPriceTypeName = "Alış",
                            UnitPrice = 12.5,
                            IsDefault = true
                        }
                    ]
                }
            });
        _actQuery
            .Setup(q => q.GetAllActsAsync(
                AccessToken,
                CompanyId,
                It.Is<IReadOnlyList<int>>(t => t != null && t.Contains(40)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondActDto>
            {
                new()
                {
                    Id = carrierId,
                    CompanyId = CompanyId,
                    Type = 40,
                    Name = "Taşıyıcı"
                }
            });
        _actQuery
            .Setup(q => q.GetActAddressesAsync(AccessToken, actId, CompanyId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondActAddressDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ActId = actId,
                    Type = 20,
                    CountryId = 1,
                    CityId = 6,
                    Street = "Test Cad.",
                    PostalZone = "06000"
                }
            });
        _despatchCommand.Setup(c => c.CreateIncomingDraftAsync(AccessToken, It.IsAny<SysmondIncomingDespatchCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(despatchId);
        _despatchCommand.Setup(c => c.CreateIncomingItemAsync(AccessToken, It.IsAny<SysmondDespatchItemCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemId);
        _despatchCommand.Setup(c => c.SaveIncomingAsync(AccessToken, It.IsAny<SysmondIncomingDespatchSaveDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        PurchaseOrder? added = null;
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseOrder, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateIncomingDespatchAsync(CompanyId, AccessToken, request);

        result.ExternalSysmondId.Should().Be(despatchId);
        result.DocumentType.Should().Be(PurchaseOrderDocumentType.IncomingDespatch);
        result.Direction.Should().Be(DespatchDirection.Incoming);
        result.Status.Should().Be(PurchaseOrderStatus.Saved);
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be(product.Id);
        result.Items[0].Quantity.Should().Be(3);
        result.TotalAmount.Should().Be(37.5m);

        added.Should().NotBeNull();
        added!.ExternalSysmondCompanyPeriodId.Should().Be(PeriodId);
        added.Items.First().ExternalSysmondId.Should().Be(itemId);

        _despatchCommand.Verify(
            c => c.CreateIncomingDraftAsync(
                AccessToken,
                It.Is<SysmondIncomingDespatchCreateDto>(d =>
                    d.CompanyPeriodId == PeriodId
                    && d.CarrierId == carrierId
                    && d.DeliveryAddressCreateDto != null
                    && d.DeliveryAddressCreateDto.Address!.Street == "Test Cad."
                    && d.DespatchPartyCreateDtos != null
                    && d.DespatchPartyCreateDtos.Count == 3
                    && d.DespatchPartyCreateDtos[0].ActId == actId
                    && d.DespatchPartyCreateDtos[0].Type == 30
                    && d.DespatchPartyCreateDtos.Any(p => p.Type == 10 && p.ActVknTckn == "52819614916")
                    && d.DespatchPartyCreateDtos.Any(p => p.Type == 20 && p.ActVknTckn == "52819614916")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _despatchCommand.Verify(
            c => c.CreateIncomingItemAsync(
                AccessToken,
                It.Is<SysmondDespatchItemCreateDto>(i =>
                    i.DespatchId == despatchId
                    && i.StockId == stockExt
                    && i.WarehouseId == whExt
                    && i.Quantity == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _despatchCommand.Verify(
            c => c.SaveIncomingAsync(
                AccessToken,
                It.Is<SysmondIncomingDespatchSaveDto>(s =>
                    s.CompanyId == CompanyId && s.DespatchId == despatchId && s.Recalculate),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncomingDespatchAsync_WhenNoItems_ThrowsValidation()
    {
        var act = async () => await _sut.CreateIncomingDespatchAsync(
            CompanyId,
            AccessToken,
            new SysmondCreateIncomingDespatchRequest { ActId = Guid.NewGuid() });

        await act.Should().ThrowAsync<Core.Validations.ValidationException>();
        _despatchCommand.Verify(
            c => c.CreateIncomingDraftAsync(It.IsAny<string>(), It.IsAny<SysmondIncomingDespatchCreateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOutgoingDespatchAsync_CallsOutgoingDraftItemSave_AndWritesLocalOrder()
    {
        var company = SysmondSyncServiceTestHelper.CreateCompany(CompanyId);
        company.TaxNumber = "52819614916";
        company.TaxOffice = "NİLÜFER VERGİ DAİRESİ MÜD.";
        var user = SysmondSyncServiceTestHelper.CreateUser(CompanyId);
        var actId = Guid.NewGuid();
        var companyAddressId = Guid.Parse("8849b16c-cb0b-e039-fe00-3a22008d01af");
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var measureUnitId = Guid.NewGuid();
        var salePriceId = Guid.NewGuid();
        var despatchId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = stockExt,
            Sku = "SKU-OUT",
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

        var request = new SysmondCreateOutgoingDespatchRequest
        {
            ActId = actId,
            ActName = "Salih",
            ActVknTckn = "31996760944",
            CompanyAddressId = companyAddressId,
            DocNo = "OUT-1",
            DeliveryAddress = new SysmondDespatchDeliveryAddressCreateDto
            {
                Address = new SysmondAddressCreateDto
                {
                    Type = 10,
                    CountryId = 1,
                    CityOther = "Ankara",
                    Street = "Deneme Cad. No:1"
                }
            },
            Items =
            [
                new SysmondCreateIncomingDespatchItemRequest
                {
                    StockId = stockExt,
                    WarehouseId = whExt,
                    MeasureUnitId = measureUnitId,
                    Quantity = 2,
                    UnitPrice = 50,
                    VatPercent = 20,
                    Name = "Kalem"
                }
            ]
        };

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });
        _actRepository.Setup(r => r.GetByExternalSysmondIdAsync(actId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Act
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ExternalSysmondId = actId,
                Type = 30,
                Name = "Salih",
                VknTckn = "31996760944",
                SyncedAt = DateTime.UtcNow
            });
        _actAddressRepository.Setup(r => r.GetByActIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ActAddress>());
        _stockQuery
            .Setup(q => q.GetAllStocksAsync(AccessToken, CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondStockDto>
            {
                new()
                {
                    Id = stockExt,
                    CompanyId = CompanyId,
                    Code = "SKU-OUT",
                    Name = "Ürün",
                    Type = 10,
                    IsActive = true,
                    Prices =
                    [
                        new SysmondStockPriceDto
                        {
                            Id = salePriceId,
                            StockId = stockExt,
                            StockPriceTypeName = "Satış",
                            UnitPrice = 50,
                            IsDefault = true
                        }
                    ]
                }
            });
        _despatchCommand.Setup(c => c.CreateOutgoingDraftAsync(AccessToken, It.IsAny<SysmondOutgoingDespatchCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(despatchId);
        _despatchCommand.Setup(c => c.CreateOutgoingItemAsync(AccessToken, It.IsAny<SysmondDespatchItemCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemId);
        _despatchCommand.Setup(c => c.SaveOutgoingAsync(AccessToken, It.IsAny<SysmondIncomingDespatchSaveDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _poRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateOutgoingDespatchAsync(CompanyId, AccessToken, request);

        result.DocumentType.Should().Be(PurchaseOrderDocumentType.OutgoingDespatch);
        result.Direction.Should().Be(DespatchDirection.Outgoing);
        result.ExternalSysmondId.Should().Be(despatchId);
        result.Items.Should().HaveCount(1);

        _despatchCommand.Verify(
            c => c.CreateOutgoingDraftAsync(
                AccessToken,
                It.Is<SysmondOutgoingDespatchCreateDto>(d =>
                    d.CompanyAddressId == companyAddressId
                    && d.CurrencyId == 949
                    && d.TemplateId == PaperDespatchTemplateId
                    && d.Scenario == 30
                    && d.Type == 10
                    && d.DeliveryAddressCreateDto != null
                    && d.DespatchPartyCreateDtos!.Count == 3
                    && d.DespatchPartyCreateDtos.Any(p => p.Type == 30 && p.ActVknTckn == "52819614916")
                    && d.DespatchPartyCreateDtos.Any(p => p.Type == 10 && p.ActName == "Salih" && p.ActId == actId)
                    && d.DespatchPartyCreateDtos.Any(p =>
                        p.Type == 20
                        && p.ActName == "Salih"
                        && p.ActVknTckn == "31996760944"
                        && p.ActTaxOfficeName == "Ankara VD"
                        && p.ActId == actId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _despatchCommand.Verify(
            c => c.CreateOutgoingItemAsync(
                AccessToken,
                It.Is<SysmondDespatchItemCreateDto>(i =>
                    i.DespatchId == despatchId
                    && i.StockId == stockExt
                    && i.StockPriceId == salePriceId
                    && i.Quantity == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
