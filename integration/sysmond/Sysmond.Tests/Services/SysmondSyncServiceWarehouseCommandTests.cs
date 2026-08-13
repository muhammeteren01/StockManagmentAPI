using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Repositories;
using Integration.Sysmond.Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>SysmondSyncService CreateWarehouse / UpdateWarehouse birim testleri.</summary>
public class SysmondSyncServiceWarehouseCommandTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Integration.Sysmond.Service.Services.SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");

    public SysmondSyncServiceWarehouseCommandTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork);

        _companyRepository
            .Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(CompanyId));
    }

    [Fact]
    public async Task CreateWarehouseAsync_WhenValid_CallsSysmondAndPersistsLocalWarehouse()
    {
        var remoteWarehouseId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var request = new SysmondCreateWarehouseRequest
        {
            Name = "Ana Depo",
            WarehouseCode = "WH-01"
        };

        _inventoryQuery
            .Setup(q => q.CreateWarehouseAsync(
                AccessToken,
                It.Is<SysmondWarehouseCreateDto>(d =>
                    d.CompanyId == CompanyId &&
                    d.Name == "Ana Depo" &&
                    d.WarehouseCode == "WH-01"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteWarehouseId);

        Warehouse? captured = null;
        _warehouseRepository
            .Setup(r => r.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()))
            .Callback<Warehouse, CancellationToken>((w, _) => captured = w)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateWarehouseAsync(CompanyId, AccessToken, request, CancellationToken.None);

        result.Name.Should().Be("Ana Depo");
        result.Location.Should().Be("WH-01");
        captured.Should().NotBeNull();
        captured!.ExternalSysmondId.Should().Be(remoteWarehouseId);
        captured.CompanyId.Should().Be(CompanyId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWarehouseAsync_WhenNameAndCodeMissing_ThrowsValidationException()
    {
        var request = new SysmondCreateWarehouseRequest();

        var act = async () => await _sut.CreateWarehouseAsync(
            CompanyId, AccessToken, request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        _inventoryQuery.Verify(
            q => q.CreateWarehouseAsync(
                It.IsAny<string>(), It.IsAny<SysmondWarehouseCreateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WhenWarehouseExists_UpdatesRemoteAndLocal()
    {
        var remoteWarehouseId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = remoteWarehouseId,
            Name = "Eski Depo",
            Location = "OLD",
            IsActive = true
        };

        _warehouseRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        var request = new SysmondUpdateWarehouseRequest { Name = "Yeni Depo", WarehouseCode = "NEW" };

        var result = await _sut.UpdateWarehouseAsync(
            CompanyId, AccessToken, remoteWarehouseId, request, CancellationToken.None);

        result.Name.Should().Be("Yeni Depo");
        result.Location.Should().Be("NEW");
        warehouse.Name.Should().Be("Yeni Depo");
        warehouse.Location.Should().Be("NEW");
        _inventoryQuery.Verify(
            q => q.UpdateWarehouseAsync(
                AccessToken,
                It.Is<SysmondWarehouseUpdateDto>(d =>
                    d.Id == remoteWarehouseId && d.Name == "Yeni Depo" && d.WarehouseCode == "NEW"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _warehouseRepository.Verify(r => r.Update(warehouse), Times.Once);
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WhenNoFieldsProvided_ThrowsValidationException()
    {
        var remoteWarehouseId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        _warehouseRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Warehouse
            {
                Id = Guid.NewGuid(),
                CompanyId = CompanyId,
                ExternalSysmondId = remoteWarehouseId,
                Name = "Depo",
                Location = "L1"
            });

        var act = async () => await _sut.UpdateWarehouseAsync(
            CompanyId,
            AccessToken,
            remoteWarehouseId,
            new SysmondUpdateWarehouseRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _inventoryQuery.Verify(
            q => q.UpdateWarehouseAsync(
                It.IsAny<string>(), It.IsAny<SysmondWarehouseUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
