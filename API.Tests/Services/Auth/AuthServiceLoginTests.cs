using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services.Auth;

namespace API.Tests.Services.Auth;

/// <summary>AuthService.LoginAsync birim testleri (gerçek validator + mock bağımlılıklar).</summary>
public class AuthServiceLoginTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AuthService _sut;

    public AuthServiceLoginTests()
    {
        _sut = AuthServiceTestHelper.CreateSut(
            _userRepository, _unitOfWork, _passwordService, _tokenService, _currentUser);
    }

    /// <summary>Login: e-posta boş → ValidationException.</summary>
    [Fact]
    public async Task LoginAsync_WhenEmailEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidLogin(email: "");

        var act = async () => await _sut.LoginAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Email));
        _userRepository.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Login: geçersiz e-posta formatı → ValidationException.</summary>
    [Fact]
    public async Task LoginAsync_WhenEmailFormatInvalid_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidLogin(email: "not-an-email");

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
        _userRepository.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Login: şifre boş → ValidationException.</summary>
    [Fact]
    public async Task LoginAsync_WhenPasswordEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidLogin(password: "");

        var act = async () => await _sut.LoginAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Password));
    }

    /// <summary>Login: kullanıcı bulunamadı → UnauthorizedException.</summary>
    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("E-posta veya şifre hatalı.");
        _passwordService.Verify(
            p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>Login: yanlış şifre → UnauthorizedException.</summary>
    [Fact]
    public async Task LoginAsync_WhenPasswordWrong_ThrowsUnauthorizedException()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        var user = AuthServiceTestHelper.CreateUser(email: request.Email);
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService
            .Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("E-posta veya şifre hatalı.");
    }

    /// <summary>Login: pasif kullanıcı → UnauthorizedException.</summary>
    [Fact]
    public async Task LoginAsync_WhenUserInactive_ThrowsUnauthorizedException()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        var user = AuthServiceTestHelper.CreateUser(email: request.Email, isActive: false);
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService
            .Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Kullanıcı hesabı pasif.");
        _tokenService.Verify(
            t => t.CreateToken(It.IsAny<User>(), out It.Ref<DateTime>.IsAny),
            Times.Never);
    }

    /// <summary>Login: geçerli kimlik → AuthResponse (token alanları dolu).</summary>
    [Fact]
    public async Task LoginAsync_WhenSuccessful_ReturnsAuthResponse()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        var user = AuthServiceTestHelper.CreateUser(email: request.Email);
        _userRepository
            .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService
            .Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        AuthServiceTestHelper.SetupToken(_tokenService, "login-jwt");

        var result = await _sut.LoginAsync(request);

        result.Token.Should().Be("login-jwt");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        result.UserId.Should().Be(user.Id);
        result.CompanyId.Should().Be(user.CompanyId);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.Role.Should().Be(user.Role);
        _tokenService.Verify(
            t => t.CreateToken(user, out It.Ref<DateTime>.IsAny),
            Times.Once);
    }
}
