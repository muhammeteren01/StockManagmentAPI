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

/// <summary>AuthController.Register birim testleri.</summary>
public class AuthControllerRegisterTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    public AuthControllerRegisterTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    /// <summary>
    /// Register: servis başarılı AuthResponse döndüğünde Ok(200) ve aynı body döner;
    /// RegisterAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Register_WhenServiceSucceeds_ReturnsOkWithAuthResponse()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali@test.com",
            Password = "Secret1!",
            CompanyId = Guid.NewGuid(),
            Role = UserRole.Staff
        };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.Staff, request.CompanyId);
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Register(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _authService.Verify(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Register: CompanyId null (SuperAdmin) iken servis başarılıysa Ok döner.
    /// </summary>
    [Fact]
    public async Task Register_WhenSuperAdmin_CompanyIdNull_ReturnsOk()
    {
        var request = new RegisterRequest
        {
            FirstName = "Root",
            LastName = "Admin",
            Email = "root@test.com",
            Password = "Secret1!",
            CompanyId = null,
            Role = UserRole.SuperAdmin
        };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.SuperAdmin, companyId: null);
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Register(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((AuthResponse)ok.Value!).CompanyId.Should().BeNull();
        ((AuthResponse)ok.Value!).Role.Should().Be(UserRole.SuperAdmin);
    }

    /// <summary>
    /// Register: CompanyId null ama rol SuperAdmin değil — controller reddetmez, Ok iletir.
    /// </summary>
    [Theory]
    [InlineData(UserRole.CompanyAdmin)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Staff)]
    public async Task Register_WhenCompanyIdNull_AndNotSuperAdmin_ReturnsOkWithNullCompanyId(UserRole role)
    {
        var request = new RegisterRequest
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = $"{role}@test.com",
            Password = "Secret1!",
            CompanyId = null,
            Role = role
        };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, role, companyId: null);
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Register(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (AuthResponse)ok.Value!;
        body.Role.Should().Be(role);
        body.CompanyId.Should().BeNull();
        body.Role.Should().NotBe(UserRole.SuperAdmin);
    }

    /// <summary>
    /// Register: yanıtta Role gelmemiş / varsayılan (0). Controller Role doğrulamaz; Ok ile iletir.
    /// </summary>
    [Fact]
    public async Task Register_WhenRoleMissingOrDefault_ReturnsOkWithDefaultRole()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = "norole@test.com",
            Password = "Secret1!",
            CompanyId = Guid.NewGuid(),
            Role = default
        };
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, role: default, companyId: request.CompanyId);
        expected.Role.Should().Be((UserRole)0);

        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Register(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (AuthResponse)ok.Value!;
        body.Role.Should().Be((UserRole)0);
        Enum.IsDefined(body.Role).Should().BeFalse();
    }

    /// <summary>
    /// Register: e-posta boş/null/whitespace → ValidationException iletilir.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_WhenEmailEmptyOrNull_ThrowsValidationException(string? email)
    {
        var request = new RegisterRequest
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = email!,
            Password = "Secret1!"
        };
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(RegisterRequest.Email), "E-posta zorunludur.")
            ]));

        var act = async () => await _sut.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Register: şifre boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Register_WhenPasswordEmpty_ThrowsValidationException()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali@test.com",
            Password = ""
        };
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(RegisterRequest.Password), "Şifre zorunludur.")
            ]));

        var act = async () => await _sut.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Register: e-posta zaten kayıtlı → ConflictException iletilir (HTTP 409).
    /// </summary>
    [Fact]
    public async Task Register_WhenEmailAlreadyRegistered_ThrowsConflictException()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali@test.com",
            Password = "Secret1!"
        };
        _authService
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Bu e-posta zaten kayıtlı."));

        var act = async () => await _sut.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Bu e-posta zaten kayıtlı.");
    }
}
