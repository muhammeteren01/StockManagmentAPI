using Core.Entities;
using Core.Validations.PurchaseOrders;
using FluentAssertions;

namespace API.Tests.Validations.PurchaseOrders;

/// <summary>PurchaseOrderItemValidator birim testleri.</summary>
public class PurchaseOrderItemValidatorTests
{
    private readonly PurchaseOrderItemValidator _sut = new();

    private static PurchaseOrderItem ValidItem() => new()
    {
        Id = Guid.NewGuid(),
        PurchaseOrderId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        Quantity = 5,
        UnitPrice = 12.5m,
        ReceivedQuantity = 0
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
            e.PropertyName == nameof(PurchaseOrderItem.ProductId) &&
            e.ErrorMessage == "Ürün zorunludur.");
    }

    /// <summary>Quantity 0 → geçerli (irsaliye kalemleri).</summary>
    [Fact]
    public void Validate_WhenQuantityZero_Succeeds()
    {
        var item = ValidItem();
        item.Quantity = 0;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Quantity negatif → hata.</summary>
    [Fact]
    public void Validate_WhenQuantityNegative_ReturnsError()
    {
        var item = ValidItem();
        item.Quantity = -3;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrderItem.Quantity) &&
            e.ErrorMessage == "Miktar negatif olamaz.");
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

    /// <summary>UnitPrice negatif → hata.</summary>
    [Fact]
    public void Validate_WhenUnitPriceNegative_ReturnsError()
    {
        var item = ValidItem();
        item.UnitPrice = -1m;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrderItem.UnitPrice) &&
            e.ErrorMessage == "Birim fiyat negatif olamaz.");
    }

    /// <summary>UnitPrice 0 → geçerli.</summary>
    [Fact]
    public void Validate_WhenUnitPriceZero_Succeeds()
    {
        var item = ValidItem();
        item.UnitPrice = 0m;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>ReceivedQuantity negatif → hata.</summary>
    [Fact]
    public void Validate_WhenReceivedQuantityNegative_ReturnsError()
    {
        var item = ValidItem();
        item.ReceivedQuantity = -1;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrderItem.ReceivedQuantity) &&
            e.ErrorMessage == "Teslim alınan miktar negatif olamaz.");
    }

    /// <summary>ReceivedQuantity Quantity'yi aşarsa → hata.</summary>
    [Fact]
    public void Validate_WhenReceivedExceedsQuantity_ReturnsError()
    {
        var item = ValidItem();
        item.Quantity = 5;
        item.ReceivedQuantity = 6;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrderItem.ReceivedQuantity) &&
            e.ErrorMessage == "Teslim alınan miktar sipariş miktarını aşamaz.");
    }

    /// <summary>ReceivedQuantity = Quantity → geçerli.</summary>
    [Fact]
    public void Validate_WhenReceivedEqualsQuantity_Succeeds()
    {
        var item = ValidItem();
        item.Quantity = 5;
        item.ReceivedQuantity = 5;

        var result = _sut.Validate(item);

        result.IsValid.Should().BeTrue();
    }
}
