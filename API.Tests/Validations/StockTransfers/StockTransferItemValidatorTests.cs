using Core.Entities;
using Core.Validations.StockTransfers;
using FluentAssertions;

namespace API.Tests.Validations.StockTransfers;

/// <summary>StockTransferItemValidator birim testleri.</summary>
public class StockTransferItemValidatorTests
{
    private readonly StockTransferItemValidator _sut = new();

    private static StockTransferItem ValidItem() => new()
    {
        Id = Guid.NewGuid(),
        TransferId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        Quantity = 5
    };

    /// <summary>Geçerli kalem → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenItemValid_Succeeds()
    {
        var result = _sut.Validate(ValidItem());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>ProductId boş → hata.</summary>
    [Fact]
    public void Validate_WhenProductIdEmpty_ReturnsError()
    {
        var item = ValidItem();
        item.ProductId = Guid.Empty;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransferItem.ProductId) &&
            e.ErrorMessage == "Ürün zorunludur.");
    }

    /// <summary>Quantity 0 veya negatif → hata.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_WhenQuantityNotPositive_ReturnsError(int quantity)
    {
        var item = ValidItem();
        item.Quantity = quantity;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransferItem.Quantity) &&
            e.ErrorMessage == "Transfer miktarı pozitif olmalıdır.");
    }

    /// <summary>Quantity 1 → geçerli.</summary>
    [Fact]
    public void Validate_WhenQuantityOne_Succeeds()
    {
        var item = ValidItem();
        item.Quantity = 1;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeTrue();
    }
}
