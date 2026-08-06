using Core.Entities;
using Core.Validations.Inventories;
using FluentAssertions;

namespace API.Tests.Validations.Inventories;

/// <summary>
/// InventoryValidator birim testleri (entity kuralları).
/// InventoryService request DTO validator kullanmaz; stok yazımı diğer servislerde.
/// </summary>
public class InventoryValidatorTests
{
    private readonly InventoryValidator _sut = new();

    private static Inventory ValidEntity() => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        Quantity = 10,
        LastUpdated = DateTime.UtcNow,
        RowVersion = [1, 2, 3, 4]
    };

    /// <summary>Geçerli entity → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenEntityValid_Succeeds()
    {
        var result = _sut.Validate(ValidEntity());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>Quantity 0 → geçerli (>= 0).</summary>
    [Fact]
    public void Validate_WhenQuantityZero_Succeeds()
    {
        var entity = ValidEntity();
        entity.Quantity = 0;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Quantity negatif → hata.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenQuantityNegative_ReturnsError(int quantity)
    {
        var entity = ValidEntity();
        entity.Quantity = quantity;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Inventory.Quantity) &&
            e.ErrorMessage == "Stok miktarı negatif olamaz.");
    }

    /// <summary>ProductId boş → hata.</summary>
    [Fact]
    public void Validate_WhenProductIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.ProductId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Inventory.ProductId) &&
            e.ErrorMessage == "Ürün zorunludur.");
    }

    /// <summary>WarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenWarehouseIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.WarehouseId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(Inventory.WarehouseId) &&
            e.ErrorMessage == "Depo zorunludur.");
    }
}
