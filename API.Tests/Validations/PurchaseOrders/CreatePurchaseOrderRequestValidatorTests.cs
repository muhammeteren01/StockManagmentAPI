using Core.DTOs.PurchaseOrders;
using Core.Validations.PurchaseOrders;
using FluentAssertions;

namespace API.Tests.Validations.PurchaseOrders;

/// <summary>CreatePurchaseOrderRequestValidator birim testleri (kalemler dahil).</summary>
public class CreatePurchaseOrderRequestValidatorTests
{
    private readonly CreatePurchaseOrderRequestValidator _sut = new();

    private static CreatePurchaseOrderRequest ValidRequest() => new()
    {
        SupplierId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        OrderNumber = "PO-001",
        ExpectedDeliveryDate = DateTime.UtcNow.AddDays(3),
        Items =
        [
            new CreatePurchaseOrderItemRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity = 5,
                UnitPrice = 10m
            }
        ]
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>SupplierId boş → hata.</summary>
    [Fact]
    public void Validate_WhenSupplierIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.SupplierId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePurchaseOrderRequest.SupplierId));
    }

    /// <summary>WarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenWarehouseIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.WarehouseId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePurchaseOrderRequest.WarehouseId));
    }

    /// <summary>OrderNumber boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenOrderNumberEmpty_ReturnsError(string? orderNumber)
    {
        var request = ValidRequest();
        request.OrderNumber = orderNumber!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePurchaseOrderRequest.OrderNumber));
    }

    /// <summary>OrderNumber 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenOrderNumberTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.OrderNumber = new string('P', 101);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePurchaseOrderRequest.OrderNumber));
    }

    /// <summary>OrderNumber 100 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenOrderNumberAtMaxLength_Succeeds()
    {
        var request = ValidRequest();
        request.OrderNumber = new string('P', 100);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Items boş → hata.</summary>
    [Fact]
    public void Validate_WhenItemsEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.Items.Clear();

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePurchaseOrderRequest.Items));
    }

    /// <summary>Kalem ProductId boş → hata.</summary>
    [Fact]
    public void Validate_WhenItemProductIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.Items[0].ProductId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items[0].ProductId");
    }

    /// <summary>Kalem Quantity 0 veya negatif → hata.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenItemQuantityNotPositive_ReturnsError(int quantity)
    {
        var request = ValidRequest();
        request.Items[0].Quantity = quantity;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items[0].Quantity");
    }

    /// <summary>Kalem Quantity 1 → geçerli.</summary>
    [Fact]
    public void Validate_WhenItemQuantityOne_Succeeds()
    {
        var request = ValidRequest();
        request.Items[0].Quantity = 1;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Kalem UnitPrice negatif → hata.</summary>
    [Fact]
    public void Validate_WhenItemUnitPriceNegative_ReturnsError()
    {
        var request = ValidRequest();
        request.Items[0].UnitPrice = -0.01m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items[0].UnitPrice");
    }

    /// <summary>Kalem UnitPrice 0 → geçerli.</summary>
    [Fact]
    public void Validate_WhenItemUnitPriceZero_Succeeds()
    {
        var request = ValidRequest();
        request.Items[0].UnitPrice = 0m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>CompanyId null → geçerli (CompanyAdmin token kullanır).</summary>
    [Fact]
    public void Validate_WhenCompanyIdNull_Succeeds()
    {
        var request = ValidRequest();
        request.CompanyId = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>ExpectedDeliveryDate null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenExpectedDeliveryDateNull_Succeeds()
    {
        var request = ValidRequest();
        request.ExpectedDeliveryDate = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
