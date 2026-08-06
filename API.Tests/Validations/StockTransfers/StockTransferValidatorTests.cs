using Core.Entities;
using Core.Enums;
using Core.Validations.StockTransfers;
using FluentAssertions;

namespace API.Tests.Validations.StockTransfers;

/// <summary>StockTransferValidator birim testleri (entity kuralları).</summary>
public class StockTransferValidatorTests
{
    private readonly StockTransferValidator _sut = new();

    private static StockTransfer ValidEntity() => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        FromWarehouseId = Guid.NewGuid(),
        ToWarehouseId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Status = StockTransferStatus.Pending,
        ReferenceNo = "TR-001",
        TransferDate = DateTime.UtcNow,
        Items =
        [
            new StockTransferItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 5
            }
        ]
    };

    /// <summary>Geçerli entity → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenEntityValid_Succeeds()
    {
        var result = _sut.Validate(ValidEntity());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>FromWarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenFromWarehouseIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.FromWarehouseId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.FromWarehouseId) &&
            e.ErrorMessage == "Kaynak depo zorunludur.");
    }

    /// <summary>ToWarehouseId boş → hata.</summary>
    [Fact]
    public void Validate_WhenToWarehouseIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.ToWarehouseId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.ToWarehouseId) &&
            e.ErrorMessage == "Hedef depo zorunludur.");
    }

    /// <summary>Kaynak = hedef → hata.</summary>
    [Fact]
    public void Validate_WhenFromEqualsToWarehouse_ReturnsError()
    {
        var entity = ValidEntity();
        entity.ToWarehouseId = entity.FromWarehouseId;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.ToWarehouseId) &&
            e.ErrorMessage == "Kaynak ve hedef depo aynı olamaz.");
    }

    /// <summary>UserId boş → hata.</summary>
    [Fact]
    public void Validate_WhenUserIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.UserId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.UserId) &&
            e.ErrorMessage == "Kullanıcı zorunludur.");
    }

    /// <summary>ReferenceNo boş → hata.</summary>
    [Fact]
    public void Validate_WhenReferenceNoEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.ReferenceNo = "";

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.ReferenceNo) &&
            e.ErrorMessage == "Referans no zorunludur.");
    }

    /// <summary>ReferenceNo 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenReferenceNoTooLong_ReturnsError()
    {
        var entity = ValidEntity();
        entity.ReferenceNo = new string('R', 101);

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.ReferenceNo) &&
            e.ErrorMessage == "Referans no en fazla 100 karakter olabilir.");
    }

    /// <summary>Status enum dışı → hata.</summary>
    [Fact]
    public void Validate_WhenStatusOutOfEnum_ReturnsError()
    {
        var entity = ValidEntity();
        entity.Status = (StockTransferStatus)999;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.Status) &&
            e.ErrorMessage == "Geçersiz transfer durumu.");
    }

    /// <summary>Tüm Status değerleri → geçerli.</summary>
    [Theory]
    [InlineData(StockTransferStatus.Pending)]
    [InlineData(StockTransferStatus.InTransit)]
    [InlineData(StockTransferStatus.Completed)]
    [InlineData(StockTransferStatus.Cancelled)]
    public void Validate_WhenStatusValid_Succeeds(StockTransferStatus status)
    {
        var entity = ValidEntity();
        entity.Status = status;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Items boş → hata.</summary>
    [Fact]
    public void Validate_WhenItemsEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.Items.Clear();

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(StockTransfer.Items) &&
            e.ErrorMessage == "Transfer en az bir kalem içermelidir.");
    }

    /// <summary>Kalem Quantity pozitif değil → hata (RuleForEach).</summary>
    [Fact]
    public void Validate_WhenItemQuantityNotPositive_ReturnsError()
    {
        var entity = ValidEntity();
        entity.Items.First().Quantity = 0;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Items[0].Quantity" &&
            e.ErrorMessage == "Transfer miktarı pozitif olmalıdır.");
    }
}
