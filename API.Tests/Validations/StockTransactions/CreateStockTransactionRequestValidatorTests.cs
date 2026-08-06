using Core.DTOs.StockTransactions;
using Core.Enums;
using Core.Validations.StockTransactions;
using FluentAssertions;

namespace API.Tests.Validations.StockTransactions;

/// <summary>CreateStockTransactionRequestValidator birim testleri.</summary>
public class CreateStockTransactionRequestValidatorTests
{
    private readonly CreateStockTransactionRequestValidator _sut = new();

    private static CreateStockTransactionRequest ValidRequest() => new()
    {
        ProductId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        TransactionType = TransactionType.In,
        Quantity = 10,
        ReasonCode = ReasonCode.Damage,
        ReferenceNo = "REF-001",
        Notes = "Not"
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>ProductId boş → hata.</summary>
    [Fact]
    public void Validate_WhenProductIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.ProductId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStockTransactionRequest.ProductId));
    }

    /// <summary>WarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenWarehouseIdEmpty_ReturnsError()
    {
        var request = ValidRequest();
        request.WarehouseId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStockTransactionRequest.WarehouseId));
    }

    /// <summary>TransactionType enum dışı → hata.</summary>
    [Fact]
    public void Validate_WhenTransactionTypeOutOfEnum_ReturnsError()
    {
        var request = ValidRequest();
        request.TransactionType = (TransactionType)999;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStockTransactionRequest.TransactionType));
    }

    /// <summary>Quantity 0 veya negatif → hata.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenQuantityNotPositive_ReturnsError(int quantity)
    {
        var request = ValidRequest();
        request.Quantity = quantity;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStockTransactionRequest.Quantity));
    }

    /// <summary>Quantity 1 → geçerli.</summary>
    [Fact]
    public void Validate_WhenQuantityOne_Succeeds()
    {
        var request = ValidRequest();
        request.Quantity = 1;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>ReasonCode null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenReasonCodeNull_Succeeds()
    {
        var request = ValidRequest();
        request.ReasonCode = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>ReasonCode enum dışı → hata.</summary>
    [Fact]
    public void Validate_WhenReasonCodeOutOfEnum_ReturnsError()
    {
        var request = ValidRequest();
        request.ReasonCode = (ReasonCode)999;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStockTransactionRequest.ReasonCode));
    }

    /// <summary>ReferenceNo 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenReferenceNoTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.ReferenceNo = new string('R', 101);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStockTransactionRequest.ReferenceNo));
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

    /// <summary>ReferenceNo null → geçerli.</summary>
    [Fact]
    public void Validate_WhenReferenceNoNull_Succeeds()
    {
        var request = ValidRequest();
        request.ReferenceNo = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Tüm TransactionType değerleri → geçerli.</summary>
    [Theory]
    [InlineData(TransactionType.In)]
    [InlineData(TransactionType.Out)]
    [InlineData(TransactionType.TransferIn)]
    [InlineData(TransactionType.TransferOut)]
    [InlineData(TransactionType.Adjustment)]
    public void Validate_WhenTransactionTypeValid_Succeeds(TransactionType type)
    {
        var request = ValidRequest();
        request.TransactionType = type;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
