using Core.DTOs.Users;
using Core.Enums;
using Core.Validations.Users;
using FluentAssertions;

namespace API.Tests.Validations.Users;

/// <summary>CreateUserRequestValidator birim testleri.</summary>
public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _sut = new();

    private static CreateUserRequest ValidRequest() => new()
    {
        FirstName = "Ali",
        LastName = "Veli",
        Email = "ali@test.com",
        Password = "Secret1!",
        CompanyId = Guid.NewGuid(),
        Role = UserRole.Staff
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>FirstName boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenFirstNameEmpty_ReturnsError(string? firstName)
    {
        var request = ValidRequest();
        request.FirstName = firstName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.FirstName));
    }

    /// <summary>LastName boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenLastNameEmpty_ReturnsError(string? lastName)
    {
        var request = ValidRequest();
        request.LastName = lastName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.LastName));
    }

    /// <summary>E-posta boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenEmailEmpty_ReturnsError(string? email)
    {
        var request = ValidRequest();
        request.Email = email!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    /// <summary>Geçersiz e-posta formatı → hata.</summary>
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ali@")]
    public void Validate_WhenEmailFormatInvalid_ReturnsError(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    /// <summary>Şifre boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenPasswordEmpty_ReturnsError(string? password)
    {
        var request = ValidRequest();
        request.Password = password!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    /// <summary>Şifre 6 karakterden kısa → hata.</summary>
    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_WhenPasswordShort_ReturnsError(string password)
    {
        var request = ValidRequest();
        request.Password = password;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    /// <summary>Şifre tam 6 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenPasswordExactly6Chars_Succeeds()
    {
        var request = ValidRequest();
        request.Password = "123456";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Geçersiz Role enum → hata.</summary>
    [Fact]
    public void Validate_WhenRoleInvalid_ReturnsError()
    {
        var request = ValidRequest();
        request.Role = (UserRole)999;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Role));
    }

    /// <summary>CompanyId null → geçerli (TenantGuard serviste çözer).</summary>
    [Fact]
    public void Validate_WhenCompanyIdNull_Succeeds()
    {
        var request = ValidRequest();
        request.CompanyId = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
