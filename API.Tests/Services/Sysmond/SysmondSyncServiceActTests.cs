using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentAssertions;
using Moq;

namespace API.Tests.Services.Sysmond;

public class SysmondSyncServiceActTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondDespatchQueryService> _despatchQuery = new();
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

    public SysmondSyncServiceActTests()
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
            actQuery: _actQuery,
            actRepository: _actRepository,
            actAddressRepository: _actAddressRepository);

        _companyRepository
            .Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(CompanyId));

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _actQuery
            .Setup(q => q.GetActByIdAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysmondActDto?)null);
    }

    [Fact]
    public async Task SyncActsAsync_WhenNewActAndAddress_CreatesBoth()
    {
        var actId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var addressId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        _actQuery
            .Setup(q => q.GetAllActsAsync(
                AccessToken,
                CompanyId,
                It.IsAny<IReadOnlyList<int>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondActDto>
            {
                new()
                {
                    Id = actId,
                    CompanyId = CompanyId,
                    Type = 10,
                    Name = "earsiv test istisna",
                    VknTckn = "2222222222",
                    Scenario = 10
                }
            });

        _actQuery
            .Setup(q => q.GetActAddressesDebugAsync(
                AccessToken,
                actId,
                CompanyId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysmondActAddressDebugResult
            {
                HttpStatusCode = 200,
                ParsedCount = 1,
                Items = new List<SysmondActAddressDto>
                {
                    new()
                    {
                        Id = addressId,
                        ActId = actId,
                        Type = 10,
                        CountryId = 1,
                        Street = "Deneme Cad.",
                        CityOther = "Ankara"
                    }
                }
            });

        _actRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(actId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Act?)null);

        _actAddressRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActAddress?)null);

        _actAddressRepository
            .Setup(r => r.GetByActIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ActAddress>());

        _actRepository
            .Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Act>());

        Act? addedAct = null;
        _actRepository
            .Setup(r => r.AddAsync(It.IsAny<Act>(), It.IsAny<CancellationToken>()))
            .Callback<Act, CancellationToken>((a, _) => addedAct = a)
            .Returns(Task.CompletedTask);

        ActAddress? addedAddress = null;
        _actAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<ActAddress>(), It.IsAny<CancellationToken>()))
            .Callback<ActAddress, CancellationToken>((a, _) => addedAddress = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncActsAsync(CompanyId, AccessToken);

        result.ActsFetched.Should().Be(1);
        result.ActsCreated.Should().Be(1);
        result.AddressesFetched.Should().Be(1);
        result.AddressesCreated.Should().Be(1);
        result.Failed.Should().Be(0);

        addedAct.Should().NotBeNull();
        addedAct!.ExternalSysmondId.Should().Be(actId);
        addedAct.Name.Should().Be("earsiv test istisna");
        addedAct.VknTckn.Should().Be("2222222222");
        addedAct.Type.Should().Be(10);

        addedAddress.Should().NotBeNull();
        addedAddress!.ExternalSysmondId.Should().Be(addressId);
        addedAddress.Street.Should().Be("Deneme Cad.");
        addedAddress.ActId.Should().Be(addedAct.Id);
    }

    [Fact]
    public async Task SyncActsAsync_WhenActAddressEmpty_UsesActFullAddressFallback()
    {
        var actId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        _actQuery
            .Setup(q => q.GetAllActsAsync(
                AccessToken,
                CompanyId,
                It.IsAny<IReadOnlyList<int>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SysmondActDto>
            {
                new()
                {
                    Id = actId,
                    CompanyId = CompanyId,
                    Type = 10,
                    Name = "earsiv",
                    VknTckn = "2222222222",
                    ActFullAddress = "Atatürk Cad. No:1",
                    CityOther = "Ankara",
                    CountryId = 1
                }
            });

        _actQuery
            .Setup(q => q.GetActAddressesDebugAsync(
                AccessToken,
                actId,
                CompanyId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysmondActAddressDebugResult
            {
                HttpStatusCode = 200,
                ParsedCount = 0,
                Items = Array.Empty<SysmondActAddressDto>(),
                RawBody = """{"status":{"success":true},"data":[]}"""
            });

        _actRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(actId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Act?)null);

        _actAddressRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActAddress?)null);

        _actAddressRepository
            .Setup(r => r.GetByActIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ActAddress>());

        _actRepository
            .Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Act>());

        ActAddress? addedAddress = null;
        _actAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<ActAddress>(), It.IsAny<CancellationToken>()))
            .Callback<ActAddress, CancellationToken>((a, _) => addedAddress = a)
            .Returns(Task.CompletedTask);

        _actRepository
            .Setup(r => r.AddAsync(It.IsAny<Act>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncActsAsync(CompanyId, AccessToken);

        result.AddressesFetched.Should().Be(1);
        result.AddressesFromActFullAddress.Should().Be(1);
        result.AddressesCreated.Should().Be(1);
        addedAddress.Should().NotBeNull();
        addedAddress!.Street.Should().Be("Atatürk Cad. No:1");
        addedAddress.CityOther.Should().Be("Ankara");
    }

    [Fact]
    public async Task SyncActsAsync_WhenCompanyMissing_Throws()
    {
        _companyRepository
            .Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var act = async () => await _sut.SyncActsAsync(CompanyId, AccessToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
