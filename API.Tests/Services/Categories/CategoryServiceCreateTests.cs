using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Categories;

/// <summary>CategoryService.CreateAsync birim testleri (gerçek validator + TenantGuard).</summary>
public class CategoryServiceCreateTests
{
    private readonly Mock<ICategoryRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly CategoryService _sut;

    public CategoryServiceCreateTests()
    {
        _sut = CategoryServiceTestHelper.CreateSut(_repository, _unitOfWork, _currentUser);
    }

    /// <summary>Create: Name boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = CategoryServiceTestHelper.ValidCreate(name: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: Name 150 karakterden uzun → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenNameTooLong_ThrowsValidationException()
    {
        var request = CategoryServiceTestHelper.ValidCreate(name: new string('A', 151));

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: SuperAdmin, CompanyId yok → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        CategoryServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = CategoryServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: CompanyAdmin, token'da şirket yok → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminMissingCompanyInToken_ThrowsForbiddenException()
    {
        _currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        var request = CategoryServiceTestHelper.ValidCreate();

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }

    /// <summary>Create: CompanyAdmin — CompanyId token'dan; Add + Save + response.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminSuccessful_UsesTokenCompanyIdAndSaves()
    {
        var companyId = Guid.NewGuid();
        CategoryServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = CategoryServiceTestHelper.ValidCreate(companyId: null);
        request.CompanyId = Guid.NewGuid(); // token öncelikli; request yok sayılır

        Category? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => added = c)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(companyId);
        added.Name.Should().Be(request.Name);
        added.Description.Should().Be(request.Description);
        result.CompanyId.Should().Be(companyId);
        result.Name.Should().Be(request.Name);

        _repository.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin — request CompanyId kullanılır.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminSuccessful_UsesRequestCompanyId()
    {
        var companyId = Guid.NewGuid();
        CategoryServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = CategoryServiceTestHelper.ValidCreate(companyId: companyId);

        Category? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => added = c)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().Be(companyId);
        result.CompanyId.Should().Be(companyId);
    }
}
