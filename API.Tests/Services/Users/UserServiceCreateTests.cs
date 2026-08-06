using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Users;

/// <summary>UserService.CreateAsync birim testleri (gerçek validator + TenantGuard + mock bağımlılıklar).</summary>
public class UserServiceCreateTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly UserService _sut;

    public UserServiceCreateTests()
    {
        _sut = UserServiceTestHelper.CreateSut(
            _repository, _unitOfWork, _passwordService, _currentUser);
    }

    /// <summary>Create: e-posta boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenEmailEmpty_ThrowsValidationException()
    {
        var request = UserServiceTestHelper.ValidCreate(email: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Email));
        _repository.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: kısa şifre → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenPasswordShort_ThrowsValidationException()
    {
        var request = UserServiceTestHelper.ValidCreate();
        request.Password = "12345";

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Password));
    }

    /// <summary>Create: CompanyAdmin SuperAdmin rolü atayamaz → ForbiddenException.</summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminAssignsSuperAdmin_ThrowsForbiddenException()
    {
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, Guid.NewGuid());
        var request = UserServiceTestHelper.ValidCreate(role: UserRole.SuperAdmin);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir.");
        _repository.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: SuperAdmin, CompanyId yok (Staff) → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        UserServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = UserServiceTestHelper.ValidCreate(role: UserRole.Staff, companyId: null);
        request.CompanyId = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
    }

    /// <summary>Create: e-posta zaten kayıtlı → InvalidOperationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyRegistered_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = UserServiceTestHelper.ValidCreate(companyId: companyId);
        _repository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserServiceTestHelper.CreateEntity(email: request.Email));

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bu e-posta zaten kayıtlı: {request.Email}");
        _repository.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Create: CompanyAdmin — CompanyId token'dan; hash + AddAsync + SaveChanges + UserResponse.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenCompanyAdminSuccessful_HashesAddsSavesAndReturnsResponse()
    {
        var companyId = Guid.NewGuid();
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = UserServiceTestHelper.ValidCreate(role: UserRole.Staff, companyId: null);
        request.CompanyId = null;
        _repository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService.Setup(p => p.HashPassword(request.Password)).Returns("hashed-password");

        User? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.Email.Should().Be(request.Email);
        added.PasswordHash.Should().Be("hashed-password");
        added.CompanyId.Should().Be(companyId);
        added.Role.Should().Be(UserRole.Staff);
        result.Email.Should().Be(request.Email);
        result.CompanyId.Should().Be(companyId);

        _passwordService.Verify(p => p.HashPassword(request.Password), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: SuperAdmin, SuperAdmin rolü → CompanyId null.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuperAdminRole_CompanyIdIsNull()
    {
        UserServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = UserServiceTestHelper.ValidCreate(role: UserRole.SuperAdmin, companyId: null);
        request.CompanyId = null;
        _repository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService.Setup(p => p.HashPassword(request.Password)).Returns("hash");

        User? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().BeNull();
        result.CompanyId.Should().BeNull();
        result.Role.Should().Be(UserRole.SuperAdmin);
    }
}
