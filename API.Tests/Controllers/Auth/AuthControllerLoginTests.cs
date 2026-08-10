using API.Controllers;
using Core.DTOs.Auth;
using Core.Enums;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Auth;

/// <summary>AuthController.Login birim testleri.</summary>
public class AuthControllerLoginTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<ISysmondTokenService> _sysmondTokenService = new();
    private readonly AuthController _sut;

    public AuthControllerLoginTests()
    {
        _sut = new AuthController(_authService.Object, _sysmondTokenService.Object);
    }

    /// <summary>
    /// Login: servis başarılı AuthResponse döndüğünde Ok(200) ve aynı body döner;
    /// LoginAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Login_WhenServiceSucceeds_ReturnsOkWithAuthResponse()
    {
        var request = new LoginRequest { Email = "ali@test.com", Password = "Secret1!" };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.CompanyAdmin, Guid.NewGuid());
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Login(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _authService.Verify(s => s.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Login: CompanyId null olan SuperAdmin yanıtı Ok döner (beklenen senaryo).
    /// </summary>
    [Fact]
    public async Task Login_WhenSuperAdmin_CompanyIdNull_ReturnsOk()
    {
        var request = new LoginRequest { Email = "root@test.com", Password = "Secret1!" };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.SuperAdmin, companyId: null);
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Login(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((AuthResponse)ok.Value!).CompanyId.Should().BeNull();
        ((AuthResponse)ok.Value!).Role.Should().Be(UserRole.SuperAdmin);
    }

    /// <summary>
    /// Login: CompanyId null ama rol SuperAdmin değil (CompanyAdmin / Manager / Staff).
    /// Controller tutarsızlığı reddetmez; Ok + null CompanyId döner.
    /// </summary>
    [Theory]
    [InlineData(UserRole.CompanyAdmin)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Staff)]
    public async Task Login_WhenCompanyIdNull_AndNotSuperAdmin_ReturnsOkWithNullCompanyId(UserRole role)
    {
        var request = new LoginRequest { Email = $"{role}@test.com", Password = "Secret1!" };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, role, companyId: null);
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Login(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (AuthResponse)ok.Value!;
        body.Role.Should().Be(role);
        body.CompanyId.Should().BeNull();
        body.Role.Should().NotBe(UserRole.SuperAdmin);
    }

    /// <summary>
    /// Login: yanıtta Role gelmemiş / varsayılan (0). Controller Role doğrulamaz; Ok ile iletir.
    /// </summary>
    [Fact]
    public async Task Login_WhenRoleMissingOrDefault_ReturnsOkWithDefaultRole()
    {
        var request = new LoginRequest { Email = "norole@test.com", Password = "Secret1!" };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, role: default, companyId: Guid.NewGuid());
        expected.Role.Should().Be((UserRole)0);

        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Login(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (AuthResponse)ok.Value!;
        body.Role.Should().Be((UserRole)0);
        Enum.IsDefined(body.Role).Should().BeFalse();
    }

    /// <summary>
    /// Login: e-posta boş/null/whitespace → ValidationException iletilir.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Login_WhenEmailEmptyOrNull_ThrowsValidationException(string? email)
    {
        var request = new LoginRequest { Email = email!, Password = "Secret1!" };
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(LoginRequest.Email), "E-posta zorunludur.")
            ]));

        var act = async () => await _sut.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Login: şifre boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Login_WhenPasswordEmpty_ThrowsValidationException()
    {
        var request = new LoginRequest { Email = "ali@test.com", Password = "" };
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(LoginRequest.Password), "Şifre zorunludur.")
            ]));

        var act = async () => await _sut.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Login: hatalı e-posta/şifre → UnauthorizedException (HTTP 401).
    /// </summary>
    [Fact]
    public async Task Login_WhenCredentialsInvalid_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest { Email = "ali@test.com", Password = "yanlis" };
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("E-posta veya şifre hatalı."));

        var act = async () => await _sut.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("E-posta veya şifre hatalı.");
    }

    /// <summary>
    /// Login: pasif kullanıcı → UnauthorizedException iletilir.
    /// </summary>
    [Fact]
    public async Task Login_WhenUserInactive_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest { Email = "pasif@test.com", Password = "Secret1!" };
        _authService
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Kullanıcı hesabı pasif."));

        var act = async () => await _sut.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Kullanıcı hesabı pasif.");
    }
}
