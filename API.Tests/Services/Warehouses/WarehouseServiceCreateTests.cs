using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Warehouses;

/// <summary>WarehouseService.CreateAsync birim testleri (gerçek validator + TenantGuard).</summary>
public class WarehouseServiceCreateTests
{
    private readonly Mock<IWarehouseRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly WarehouseService _sut;

    public WarehouseServiceCreateTests()
    {
        _sut = WarehouseServiceTestHelper.CreateSut(_repository, _unitOfWork, _currentUser);
    }

    /// <summary>Create: Name boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = WarehouseServiceTestHelper.ValidCreate(name: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: Location boş → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenLocationEmpty_ThrowsValidationException()
    {
        var request = WarehouseServiceTestHelper.ValidCreate(location: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Location));
    }

    /// <summary>Create: Capacity &lt;= 0 → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCapacityNotPositive_ThrowsValidationException()
    {
        var request = WarehouseServiceTestHelper.ValidCreate(capacity: 0);

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Capacity));
    }

    /// <summary>Create: SuperAdmin, CompanyId yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        WarehouseServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = WarehouseServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin, token'da şirket yok → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminMissingCompanyInToken_ThrowsForbiddenException()
    {
        _currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        var request = WarehouseServiceTestHelper.ValidCreate();

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }

    /// <summary>Create: CompanyAdmin — CompanyId token'dan; Add + Save + response.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminSuccessful_UsesTokenCompanyIdAndSaves()
    {
        var companyId = Guid.NewGuid();
        WarehouseServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = WarehouseServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = Guid.NewGuid(); // token öncelikli; request yok sayılır

        Warehouse? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()))
            .Callback<Warehouse, CancellationToken>((w, _) => added = w)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(companyId);
        added.Name.Should().Be(request.Name);
        added.Location.Should().Be(request.Location);
        added.IsActive.Should().BeTrue();
        result.CompanyId.Should().Be(companyId);
        result.Name.Should().Be(request.Name);

        _repository.Verify(r => r.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin — request CompanyId kullanılır.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesRequestCompanyId()
    {
        var companyId = Guid.NewGuid();
        WarehouseServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = WarehouseServiceTestHelper.ValidCreate(companyId: companyId);

        Warehouse? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Warehouse>(), It.IsAny<CancellationToken>()))
            .Callback<Warehouse, CancellationToken>((w, _) => added = w)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().Be(companyId);
        result.CompanyId.Should().Be(companyId);
    }
}
