using Core.DTOs.Auth;
using Core.Validations.Auth;
using FluentAssertions;

namespace API.Tests.Validations.Auth;

/// <summary>LoginRequestValidator birim testleri (gerçek FluentValidation kuralları).</summary>
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    /// <summary>Geçerli e-posta ve şifre → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_GecerliIstek_Basarili()
    {
        var request = new LoginRequest { Email = "ali@test.com", Password = "Secret1!" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>E-posta boş/null/whitespace → "E-posta zorunludur."</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailBos_HataDoner(string? email)
    {
        var request = new LoginRequest { Email = email!, Password = "Secret1!" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(LoginRequest.Email) &&
            e.ErrorMessage == "E-posta zorunludur.");
    }

    /// <summary>Geçersiz e-posta formatı → "Geçerli bir e-posta giriniz."</summary>
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ali@")]
    [InlineData("@test.com")]
    public void Validate_EmailFormatGecersiz_HataDoner(string email)
    {
        var request = new LoginRequest { Email = email, Password = "Secret1!" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(LoginRequest.Email) &&
            e.ErrorMessage == "Geçerli bir e-posta giriniz.");
    }

    /// <summary>Şifre boş/null → "Şifre zorunludur." (Login'de minimum uzunluk kuralı yok.)</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_SifreBos_HataDoner(string? password)
    {
        var request = new LoginRequest { Email = "ali@test.com", Password = password! };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(LoginRequest.Password) &&
            e.ErrorMessage == "Şifre zorunludur.");
    }

    /// <summary>Kısa şifre Login validator'da geçerli (yalnızca NotEmpty).</summary>
    [Fact]
    public void Validate_SifreKisa_LoginKurallarindaGecerli()
    {
        var request = new LoginRequest { Email = "ali@test.com", Password = "123" };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
