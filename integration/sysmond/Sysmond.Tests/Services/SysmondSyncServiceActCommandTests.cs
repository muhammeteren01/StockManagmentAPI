using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Repositories;
using Integration.Sysmond.Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>SysmondSyncService CreateAct / UpdateAct birim testleri.</summary>
public class SysmondSyncServiceActCommandTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondActQueryService> _actQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IActRepository> _actRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Integration.Sysmond.Service.Services.SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");

    public SysmondSyncServiceActCommandTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork,
            actQuery: _actQuery,
            actRepository: _actRepository);

        _companyRepository
            .Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(CompanyId));
    }

    [Fact]
    public async Task CreateActAsync_WhenValid_CallsSysmondAndPersistsLocalAct()
    {
        var remoteActId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var request = new SysmondCreateActRequest
        {
            Type = 20,
            Name = "Test Tedarikçi",
            VknTckn = "1234567890",
            CountryId = 1
        };

        _actQuery
            .Setup(q => q.CreateActAsync(
                AccessToken,
                It.Is<SysmondActCreateDto>(d =>
                    d.CompanyId == CompanyId &&
                    d.Name == "Test Tedarikçi" &&
                    d.Type == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteActId);

        Act? captured = null;
        _actRepository
            .Setup(r => r.AddAsync(It.IsAny<Act>(), It.IsAny<CancellationToken>()))
            .Callback<Act, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateActAsync(CompanyId, AccessToken, request, CancellationToken.None);

        result.ExternalSysmondId.Should().Be(remoteActId);
        result.Name.Should().Be("Test Tedarikçi");
        result.Type.Should().Be(20);
        captured.Should().NotBeNull();
        captured!.CompanyId.Should().Be(CompanyId);
        captured.ExternalSysmondId.Should().Be(remoteActId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateActAsync_WhenNameMissing_ThrowsValidationException()
    {
        var request = new SysmondCreateActRequest { Type = 20, Name = "  " };

        var act = async () => await _sut.CreateActAsync(CompanyId, AccessToken, request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        _actQuery.Verify(
            q => q.CreateActAsync(It.IsAny<string>(), It.IsAny<SysmondActCreateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateActAsync_WhenActExists_UpdatesRemoteAndLocal()
    {
        var remoteActId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var localAct = new Act
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ExternalSysmondId = remoteActId,
            Type = 20,
            Name = "Eski Ad",
            Scenario = 30,
            SyncedAt = DateTime.UtcNow.AddDays(-1)
        };

        _actRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteActId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localAct);

        var request = new SysmondUpdateActRequest { Name = "Yeni Ad" };

        var result = await _sut.UpdateActAsync(
            CompanyId, AccessToken, remoteActId, request, CancellationToken.None);

        result.Name.Should().Be("Yeni Ad");
        localAct.Name.Should().Be("Yeni Ad");
        _actQuery.Verify(
            q => q.UpdateActAsync(
                AccessToken,
                It.Is<SysmondActUpdateDto>(d => d.Id == remoteActId && d.Name == "Yeni Ad"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _actRepository.Verify(r => r.Update(localAct), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateActAsync_WhenActNotFound_ThrowsKeyNotFoundException()
    {
        var remoteActId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        _actRepository
            .Setup(r => r.GetByExternalSysmondIdAsync(remoteActId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Act?)null);

        var act = async () => await _sut.UpdateActAsync(
            CompanyId,
            AccessToken,
            remoteActId,
            new SysmondUpdateActRequest { Name = "X" },
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _actQuery.Verify(
            q => q.UpdateActAsync(It.IsAny<string>(), It.IsAny<SysmondActUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
