using Core.DTOs.Companies;
using Core.Validations.Companies;
using FluentAssertions;

namespace API.Tests.Validations.Companies;

/// <summary>UpdateCompanyRequestValidator birim testleri.</summary>
public class UpdateCompanyRequestValidatorTests
{
    private readonly UpdateCompanyRequestValidator _sut = new();

    private static UpdateCompanyRequest ValidRequest() => new()
    {
        Name = "Acme Güncel",
        TaxOffice = "Üsküdar",
        TaxNumber = "0987654321",
        Phone = "+905559998877",
        Email = "new@acme.test",
        Address = "Ankara",
        IsActive = true
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>Name boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameEmpty_ReturnsError(string? name)
    {
        var request = ValidRequest();
        request.Name = name!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCompanyRequest.Name));
    }

    /// <summary>Name 200 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Name = new string('B', 201);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCompanyRequest.Name));
    }

    /// <summary>Geçersiz e-posta → hata.</summary>
    [Fact]
    public void Validate_WhenEmailInvalid_ReturnsError()
    {
        var request = ValidRequest();
        request.Email = "bad-email";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCompanyRequest.Email));
    }

    /// <summary>Phone 50 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenPhoneTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Phone = new string('9', 51);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCompanyRequest.Phone));
    }
}
