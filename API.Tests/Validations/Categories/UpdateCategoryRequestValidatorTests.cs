using Core.DTOs.Categories;
using Core.Validations.Categories;
using FluentAssertions;

namespace API.Tests.Validations.Categories;

/// <summary>UpdateCategoryRequestValidator birim testleri.</summary>
public class UpdateCategoryRequestValidatorTests
{
    private readonly UpdateCategoryRequestValidator _sut = new();

    private static UpdateCategoryRequest ValidRequest() => new()
    {
        Name = "Güncel Kategori",
        Description = "Güncel açıklama"
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryRequest.Name));
    }

    /// <summary>Name 150 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 151);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryRequest.Name));
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
}
