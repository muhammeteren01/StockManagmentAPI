using Core.DTOs.Warehouses;
using Core.Validations.Warehouses;
using FluentAssertions;

namespace API.Tests.Validations.Warehouses;

/// <summary>UpdateWarehouseRequestValidator birim testleri.</summary>
public class UpdateWarehouseRequestValidatorTests
{
    private readonly UpdateWarehouseRequestValidator _sut = new();

    private static UpdateWarehouseRequest ValidRequest() => new()
    {
        Name = "Güncel Depo",
        Location = "Ankara",
        Capacity = 2000,
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateWarehouseRequest.Name));
    }

    /// <summary>Location boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenLocationEmpty_ReturnsError(string? location)
    {
        var request = ValidRequest();
        request.Location = location!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateWarehouseRequest.Location));
    }

    /// <summary>Location 255 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenLocationTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Location = new string('L', 256);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateWarehouseRequest.Location));
    }

    /// <summary>Capacity &lt;= 0 → hata.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenCapacityNotPositive_ReturnsError(int capacity)
    {
        var request = ValidRequest();
        request.Capacity = capacity;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateWarehouseRequest.Capacity));
    }

    /// <summary>Capacity null → geçerli.</summary>
    [Fact]
    public void Validate_WhenCapacityNull_Succeeds()
    {
        var request = ValidRequest();
        request.Capacity = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
