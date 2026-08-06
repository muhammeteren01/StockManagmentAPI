using Core.Entities;
using Core.Enums;
using Core.Validations.PurchaseOrders;
using FluentAssertions;

namespace API.Tests.Validations.PurchaseOrders;

/// <summary>PurchaseOrderValidator birim testleri (entity kuralları).</summary>
public class PurchaseOrderValidatorTests
{
    private readonly PurchaseOrderValidator _sut = new();

    private static PurchaseOrder ValidEntity() => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        SupplierId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        OrderNumber = "PO-001",
        TotalAmount = 100m,
        Status = PurchaseOrderStatus.Pending,
        ExpectedDeliveryDate = DateTime.UtcNow.AddDays(3),
        CreatedAt = DateTime.UtcNow,
        Items =
        [
            new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 5,
                UnitPrice = 20m,
                ReceivedQuantity = 0
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

    /// <summary>CompanyId boş → hata.</summary>
    [Fact]
    public void Validate_WhenCompanyIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.CompanyId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrder.CompanyId) &&
            e.ErrorMessage == "Şirket zorunludur.");
    }

    /// <summary>SupplierId boş → hata.</summary>
    [Fact]
    public void Validate_WhenSupplierIdEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.SupplierId = Guid.Empty;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrder.SupplierId) &&
            e.ErrorMessage == "Tedarikçi zorunludur.");
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
            e.PropertyName == nameof(PurchaseOrder.WarehouseId) &&
            e.ErrorMessage == "Teslimat deposu zorunludur.");
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
            e.PropertyName == nameof(PurchaseOrder.UserId) &&
            e.ErrorMessage == "Kullanıcı zorunludur.");
    }

    /// <summary>OrderNumber boş → hata.</summary>
    [Fact]
    public void Validate_WhenOrderNumberEmpty_ReturnsError()
    {
        var entity = ValidEntity();
        entity.OrderNumber = "";

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrder.OrderNumber) &&
            e.ErrorMessage == "Sipariş numarası zorunludur.");
    }

    /// <summary>OrderNumber 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenOrderNumberTooLong_ReturnsError()
    {
        var entity = ValidEntity();
        entity.OrderNumber = new string('P', 101);

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrder.OrderNumber) &&
            e.ErrorMessage == "Sipariş numarası en fazla 100 karakter olabilir.");
    }

    /// <summary>TotalAmount negatif → hata.</summary>
    [Fact]
    public void Validate_WhenTotalAmountNegative_ReturnsError()
    {
        var entity = ValidEntity();
        entity.TotalAmount = -1m;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrder.TotalAmount) &&
            e.ErrorMessage == "Toplam tutar negatif olamaz.");
    }

    /// <summary>Status enum dışı → hata.</summary>
    [Fact]
    public void Validate_WhenStatusOutOfEnum_ReturnsError()
    {
        var entity = ValidEntity();
        entity.Status = (PurchaseOrderStatus)999;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchaseOrder.Status) &&
            e.ErrorMessage == "Geçersiz sipariş durumu.");
    }

    /// <summary>Tüm Status değerleri → geçerli.</summary>
    [Theory]
    [InlineData(PurchaseOrderStatus.Pending)]
    [InlineData(PurchaseOrderStatus.Approved)]
    [InlineData(PurchaseOrderStatus.Received)]
    [InlineData(PurchaseOrderStatus.Cancelled)]
    public void Validate_WhenStatusValid_Succeeds(PurchaseOrderStatus status)
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
            e.PropertyName == nameof(PurchaseOrder.Items) &&
            e.ErrorMessage == "Sipariş en az bir kalem içermelidir.");
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
            e.ErrorMessage == "Sipariş miktarı pozitif olmalıdır.");
    }

    /// <summary>Kalem ReceivedQuantity Quantity'yi aşarsa → hata.</summary>
    [Fact]
    public void Validate_WhenItemReceivedExceedsQuantity_ReturnsError()
    {
        var entity = ValidEntity();
        entity.Items.First().Quantity = 5;
        entity.Items.First().ReceivedQuantity = 6;

        var result = _sut.Validate(entity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Items[0].ReceivedQuantity" &&
            e.ErrorMessage == "Teslim alınan miktar sipariş miktarını aşamaz.");
    }
}
