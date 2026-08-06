using Core.DTOs.StockTransfers;
using Core.Validations.StockTransfers;
using FluentAssertions;

namespace API.Tests.Validations.StockTransfers;

/// <summary>CreateStockTransferRequestValidator birim testleri (kalemler dahil).</summary>
public class CreateStockTransferRequestValidatorTests
{
    private readonly CreateStockTransferRequestValidator _sut = new();

    private static CreateStockTransferRequest ValidRequest() => new()
    {
        FromWarehouseId = Guid.NewGuid(),
        ToWarehouseId = Guid.NewGuid(),
        ReferenceNo = "TR-001",
        Notes = "Not",
        Items =
        [
            new CreateStockTransferItemRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity = 5
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

    /// <summary>FromWarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenFromWarehouseIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.FromWarehouseId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateStockTransferRequest.FromWarehouseId));
    }

    /// <summary>ToWarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenToWarehouseIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.ToWarehouseId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateStockTransferRequest.ToWarehouseId));
    }

    /// <summary>Kaynak = hedef depo → hata.</summary>
    [Fact]
    public void Validate_WhenFromEqualsToWarehouse_ReturnsError()
    {
        var request = ValidRequest();
        request.ToWarehouseId = request.FromWarehouseId;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateStockTransferRequest.ToWarehouseId) &&
            e.ErrorMessage == "Kaynak ve hedef depo aynı olamaz.");
    }

    /// <summary>ReferenceNo boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenReferenceNoEmpty_ReturnsError(string? referenceNo)
    {
        var request = ValidRequest();
        request.ReferenceNo = referenceNo!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateStockTransferRequest.ReferenceNo));
    }

    /// <summary>ReferenceNo 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenReferenceNoTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.ReferenceNo = new string('R', 101);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateStockTransferRequest.ReferenceNo));
    }

    /// <summary>ReferenceNo 100 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenReferenceNoAtMaxLength_Succeeds()
    {
        var request = ValidRequest();
        request.ReferenceNo = new string('R', 100);

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
            e.PropertyName == nameof(CreateStockTransferRequest.Items));
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

    /// <summary>Notes null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenNotesNull_Succeeds()
    {
        var request = ValidRequest();
        request.Notes = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
