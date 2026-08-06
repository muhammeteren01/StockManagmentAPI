using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Suppliers;

/// <summary>SupplierService.CreateAsync birim testleri (gerçek validator + TenantGuard).</summary>
public class SupplierServiceCreateTests
{
    private readonly Mock<ISupplierRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly SupplierService _sut;

    public SupplierServiceCreateTests()
    {
        _sut = SupplierServiceTestHelper.CreateSut(_repository, _unitOfWork, _currentUser);
    }

    /// <summary>Create: CompanyName boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyNameEmpty_ThrowsValidationException()
    {
        var request = SupplierServiceTestHelper.ValidCreate(companyName: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.CompanyName));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyName 200 karakterden uzun → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyNameTooLong_ThrowsValidationException()
    {
        var request = SupplierServiceTestHelper.ValidCreate(companyName: new string('A', 201));

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.CompanyName));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: Email geçersiz → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenEmailInvalid_ThrowsValidationException()
    {
        var request = SupplierServiceTestHelper.ValidCreate(email: "not-an-email");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Email));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: SuperAdmin, CompanyId yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        SupplierServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = SupplierServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin, token'da şirket yok → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminMissingCompanyInToken_ThrowsForbiddenException()
    {
        _currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        var request = SupplierServiceTestHelper.ValidCreate();

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }

    /// <summary>Create: CompanyAdmin — CompanyId token'dan; Add + Save + response.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminSuccessful_UsesTokenCompanyIdAndSaves()
    {
        var companyId = Guid.NewGuid();
        SupplierServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = SupplierServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = Guid.NewGuid(); // token öncelikli; request yok sayılır

        Supplier? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((s, _) => added = s)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(companyId);
        added.CompanyName.Should().Be(request.CompanyName);
        added.ContactName.Should().Be(request.ContactName);
        added.Email.Should().Be(request.Email);
        result.CompanyId.Should().Be(companyId);
        result.CompanyName.Should().Be(request.CompanyName);

        _repository.Verify(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin — request CompanyId kullanılır.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesRequestCompanyId()
    {
        var companyId = Guid.NewGuid();
        SupplierServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = SupplierServiceTestHelper.ValidCreate(companyId: companyId);

        Supplier? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((s, _) => added = s)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().Be(companyId);
        result.CompanyId.Should().Be(companyId);
    }
}
