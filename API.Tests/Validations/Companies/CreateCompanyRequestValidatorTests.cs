using Core.DTOs.Companies;
using Core.Validations.Companies;
using FluentAssertions;

namespace API.Tests.Validations.Companies;

/// <summary>CreateCompanyRequestValidator birim testleri.</summary>
public class CreateCompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _sut = new();

    private static CreateCompanyRequest ValidRequest() => new()
    {
        Name = "Acme A.Ş.",
        TaxOffice = "Kadıköy",
        TaxNumber = "1234567890",
        Phone = "+905551112233",
        Email = "info@acme.test",
        Address = "İstanbul"
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyRequest.Name));
    }

    /// <summary>Name 200 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 201);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyRequest.Name));
    }

    /// <summary>Geçersiz e-posta → hata (Email dolu olduğunda).</summary>
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ali@")]
    public void Validate_WhenEmailInvalid_ReturnsError(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyRequest.Email));
    }

    /// <summary>Email null/boş → geçerli (opsiyonel alan).</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenEmailEmpty_Succeeds(string? email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>TaxNumber 50 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenTaxNumberTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.TaxNumber = new string('1', 51);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompanyRequest.TaxNumber));
    }
}
