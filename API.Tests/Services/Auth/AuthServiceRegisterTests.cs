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
using Service.Services.Auth;

namespace API.Tests.Services.Auth;

/// <summary>AuthService.RegisterAsync birim testleri (gerçek validator + mock bağımlılıklar).</summary>
public class AuthServiceRegisterTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AuthService _sut;

    public AuthServiceRegisterTests()
    {
        _sut = AuthServiceTestHelper.CreateSut(
            _userRepository, _unitOfWork, _passwordService, _tokenService, _currentUser);
    }

    /// <summary>Register: e-posta boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task RegisterAsync_WhenEmailEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidRegister(email: "");

        var act = async () => await _sut.RegisterAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Email));
        _userRepository.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Register: ad boş → ValidationException.</summary>
    [Fact]
    public async Task RegisterAsync_WhenFirstNameEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidRegister();
        request.FirstName = "";

        var act = async () => await _sut.RegisterAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.FirstName));
    }

    /// <summary>Register: kısa şifre → ValidationException.</summary>
    [Fact]
    public async Task RegisterAsync_WhenPasswordShort_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidRegister();
        request.Password = "12345";

        var act = async () => await _sut.RegisterAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Password));
    }

    /// <summary>Register: e-posta zaten kayıtlı → ConflictException.</summary>
    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyRegistered_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        AuthServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = AuthServiceTestHelper.ValidRegister(companyId: companyId);
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthServiceTestHelper.CreateUser(email: request.Email));

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Bu e-posta zaten kayıtlı.");
        _userRepository.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Register: CompanyAdmin çağıranı — CompanyId token'dan; hash + AddAsync + SaveChanges + AuthResponse.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WhenSuccessful_HashesAddsSavesAndReturnsAuthResponse()
    {
        var companyId = Guid.NewGuid();
        AuthServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = AuthServiceTestHelper.ValidRegister(role: UserRole.Staff, companyId: null);
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService
            .Setup(p => p.HashPassword(request.Password))
            .Returns("hashed-password");
        AuthServiceTestHelper.SetupToken(_tokenService, "register-jwt");

        User? added = null;
        _userRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.RegisterAsync(request);

        added.Should().NotBeNull();
        added!.Email.Should().Be(request.Email);
        added.FirstName.Should().Be(request.FirstName);
        added.LastName.Should().Be(request.LastName);
        added.PasswordHash.Should().Be("hashed-password");
        added.Role.Should().Be(UserRole.Staff);
        added.CompanyId.Should().Be(companyId);
        added.IsActive.Should().BeTrue();

        result.Token.Should().Be("register-jwt");
        result.Email.Should().Be(request.Email);
        result.CompanyId.Should().Be(companyId);
        result.Role.Should().Be(UserRole.Staff);
        result.UserId.Should().Be(added.Id);

        _passwordService.Verify(p => p.HashPassword(request.Password), Times.Once);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Register: SuperAdmin, SuperAdmin rolü → CompanyId null.</summary>
    [Fact]
    public async Task RegisterAsync_WhenSuperAdminRole_CompanyIdIsNull()
    {
        AuthServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = AuthServiceTestHelper.ValidRegister(role: UserRole.SuperAdmin, companyId: null);
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService.Setup(p => p.HashPassword(request.Password)).Returns("hash");
        AuthServiceTestHelper.SetupToken(_tokenService);

        User? added = null;
        _userRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.RegisterAsync(request);

        added!.CompanyId.Should().BeNull();
        result.CompanyId.Should().BeNull();
        result.Role.Should().Be(UserRole.SuperAdmin);
    }

    /// <summary>
    /// Register: CompanyAdmin SuperAdmin rolü atayamaz → ForbiddenException (TenantGuard).
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WhenCompanyAdminAssignsSuperAdmin_ThrowsForbiddenException()
    {
        AuthServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, Guid.NewGuid());
        var request = AuthServiceTestHelper.ValidRegister(role: UserRole.SuperAdmin);

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir.");
        _userRepository.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
