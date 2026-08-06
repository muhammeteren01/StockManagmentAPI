using Core.DTOs.Suppliers;
using Core.Validations.Suppliers;
using FluentAssertions;

namespace API.Tests.Validations.Suppliers;

/// <summary>CreateSupplierRequestValidator birim testleri.</summary>
public class CreateSupplierRequestValidatorTests
{
    private readonly CreateSupplierRequestValidator _sut = new();

    private static CreateSupplierRequest ValidRequest() => new()
    {
        CompanyId = Guid.NewGuid(),
        CompanyName = "Acme Tedarik",
        ContactName = "Ali Veli",
        Phone = "+905551112233",
        Email = "ali@acme.com",
        Address = "İstanbul",
        TaxNumber = "1234567890"
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>CompanyName boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenCompanyNameEmpty_ReturnsError(string? companyName)
    {
        var request = ValidRequest();
        request.CompanyName = companyName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.CompanyName));
    }

    /// <summary>CompanyName 200 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenCompanyNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.CompanyName = new string('A', 201);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.CompanyName));
    }

    /// <summary>CompanyName 200 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenCompanyNameAtMaxLength_Succeeds()
    {
        var request = ValidRequest();
        request.CompanyName = new string('A', 200);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>ContactName boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenContactNameEmpty_ReturnsError(string? contactName)
    {
        var request = ValidRequest();
        request.ContactName = contactName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.ContactName));
    }

    /// <summary>Phone boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenPhoneEmpty_ReturnsError(string? phone)
    {
        var request = ValidRequest();
        request.Phone = phone!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.Phone));
    }

    /// <summary>Email boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenEmailEmpty_ReturnsError(string? email)
    {
        var request = ValidRequest();
        request.Email = email!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.Email));
    }

    /// <summary>Email geçersiz format → hata.</summary>
    [Fact]
    public void Validate_WhenEmailInvalid_ReturnsError()
    {
        var request = ValidRequest();
        request.Email = "not-an-email";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.Email));
    }

    /// <summary>Address boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenAddressEmpty_ReturnsError(string? address)
    {
        var request = ValidRequest();
        request.Address = address!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.Address));
    }

    /// <summary>TaxNumber boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenTaxNumberEmpty_ReturnsError(string? taxNumber)
    {
        var request = ValidRequest();
        request.TaxNumber = taxNumber!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierRequest.TaxNumber));
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
