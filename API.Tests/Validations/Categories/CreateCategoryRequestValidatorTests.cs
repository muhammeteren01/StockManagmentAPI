using Core.DTOs.Categories;
using Core.Validations.Categories;
using FluentAssertions;

namespace API.Tests.Validations.Categories;

/// <summary>CreateCategoryRequestValidator birim testleri.</summary>
public class CreateCategoryRequestValidatorTests
{
    private readonly CreateCategoryRequestValidator _sut = new();

    private static CreateCategoryRequest ValidRequest() => new()
    {
        CompanyId = Guid.NewGuid(),
        Name = "Elektronik",
        Description = "Elektronik ürünler"
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryRequest.Name));
    }

    /// <summary>Name 150 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 151);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryRequest.Name));
    }

    /// <summary>Name 150 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenNameAtMaxLength_Succeeds()
    {
        var request = ValidRequest();
        request.Name = new string('A', 150);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Description null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenDescriptionNull_Succeeds()
    {
        var request = ValidRequest();
        request.Description = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
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
