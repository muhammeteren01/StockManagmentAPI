using Core.DTOs.Auth;
using Core.Enums;
using Core.Validations.Auth;
using FluentAssertions;

namespace API.Tests.Validations.Auth;

/// <summary>RegisterRequestValidator birim testleri (gerçek FluentValidation kuralları).</summary>
public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    private static RegisterRequest ValidRequest() => new()
    {
        FirstName = "Ali",
        LastName = "Veli",
        Email = "ali@test.com",
        Password = "Secret1!",
        CompanyId = Guid.NewGuid(),
        Role = UserRole.Staff
    };

    /// <summary>Tüm zorunlu alanlar dolu → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_GecerliIstek_Basarili()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>Ad boş/null/whitespace → "Ad zorunludur."</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_FirstNameBos_HataDoner(string? firstName)
    {
        var request = ValidRequest();
        request.FirstName = firstName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.FirstName) &&
            e.ErrorMessage == "Ad zorunludur.");
    }

    /// <summary>Soyad boş → "Soyad zorunludur."</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_LastNameBos_HataDoner(string? lastName)
    {
        var request = ValidRequest();
        request.LastName = lastName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.LastName) &&
            e.ErrorMessage == "Soyad zorunludur.");
    }

    /// <summary>E-posta boş → "E-posta zorunludur."</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailBos_HataDoner(string? email)
    {
        var request = ValidRequest();
        request.Email = email!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.Email) &&
            e.ErrorMessage == "E-posta zorunludur.");
    }

    /// <summary>Geçersiz e-posta formatı → "Geçerli bir e-posta giriniz."</summary>
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ali@")]
    [InlineData("@test.com")]
    public void Validate_EmailFormatGecersiz_HataDoner(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.Email) &&
            e.ErrorMessage == "Geçerli bir e-posta giriniz.");
    }

    /// <summary>Şifre boş → "Şifre zorunludur."</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_SifreBos_HataDoner(string? password)
    {
        var request = ValidRequest();
        request.Password = password!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.Password) &&
            e.ErrorMessage == "Şifre zorunludur.");
    }

    /// <summary>Şifre 6 karakterden kısa → "Şifre en az 6 karakter olmalıdır."</summary>
    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_SifreKisa_HataDoner(string password)
    {
        var request = ValidRequest();
        request.Password = password;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.Password) &&
            e.ErrorMessage == "Şifre en az 6 karakter olmalıdır.");
    }

    /// <summary>Şifre tam 6 karakter → geçerli.</summary>
    [Fact]
    public void Validate_SifreTam6Karakter_Basarili()
    {
        var request = ValidRequest();
        request.Password = "123456";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Geçersiz Role enum değeri → "Geçersiz kullanıcı rolü."</summary>
    [Fact]
    public void Validate_RoleGecersiz_HataDoner()
    {
        var request = ValidRequest();
        request.Role = (UserRole)999;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterRequest.Role) &&
            e.ErrorMessage == "Geçersiz kullanıcı rolü.");
    }
}
