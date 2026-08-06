using Core.DTOs.Warehouses;
using Core.Validations.Warehouses;
using FluentAssertions;

namespace API.Tests.Validations.Warehouses;

/// <summary>CreateWarehouseRequestValidator birim testleri.</summary>
public class CreateWarehouseRequestValidatorTests
{
    private readonly CreateWarehouseRequestValidator _sut = new();

    private static CreateWarehouseRequest ValidRequest() => new()
    {
        CompanyId = Guid.NewGuid(),
        Name = "Merkez Depo",
        Location = "İstanbul",
        Capacity = 1000
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateWarehouseRequest.Name));
    }

    /// <summary>Name 150 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 151);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateWarehouseRequest.Name));
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateWarehouseRequest.Location));
    }

    /// <summary>Capacity &lt;= 0 → hata (değer verildiğinde).</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenCapacityNotPositive_ReturnsError(int capacity)
    {
        var request = ValidRequest();
        request.Capacity = capacity;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateWarehouseRequest.Capacity));
    }

    /// <summary>Capacity null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenCapacityNull_Succeeds()
    {
        var request = ValidRequest();
        request.Capacity = null;

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
