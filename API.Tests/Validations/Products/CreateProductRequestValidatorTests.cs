using Core.DTOs.Products;
using Core.Enums;
using Core.Validations.Products;
using FluentAssertions;

namespace API.Tests.Validations.Products;

/// <summary>CreateProductRequestValidator birim testleri.</summary>
public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _sut = new();

    private static CreateProductRequest ValidRequest() => new()
    {
        CompanyId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        SupplierId = Guid.NewGuid(),
        Sku = "SKU-001",
        Barcode = "8690000000001",
        Name = "Laptop",
        Description = "15 inç",
        UnitPrice = 10000m,
        SellingPrice = 12500m,
        MinStockLevel = 5,
        Status = ProductStatus.Active
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>CategoryId boş → geçerli (opsiyonel; Sysmond Type kullanılır).</summary>
    [Fact]
    public void Validate_WhenCategoryIdEmpty_Succeeds()
    {
        var request = ValidRequest();
        request.CategoryId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>SupplierId null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenSupplierIdNull_Succeeds()
    {
        var request = ValidRequest();
        request.SupplierId = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Sku boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenSkuEmpty_ReturnsError(string? sku)
    {
        var request = ValidRequest();
        request.Sku = sku!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Sku));
    }

    /// <summary>Sku 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenSkuTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Sku = new string('S', 101);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Sku));
    }

    /// <summary>Sku 100 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenSkuAtMaxLength_Succeeds()
    {
        var request = ValidRequest();
        request.Sku = new string('S', 100);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>Barcode 100 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenBarcodeTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Barcode = new string('B', 101);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Barcode));
    }

    /// <summary>Barcode null → geçerli (opsiyonel).</summary>
    [Fact]
    public void Validate_WhenBarcodeNull_Succeeds()
    {
        var request = ValidRequest();
        request.Barcode = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Name));
    }

    /// <summary>Name 200 karakterden uzun → hata.</summary>
    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 201);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Name));
    }

    /// <summary>Name 200 karakter → geçerli.</summary>
    [Fact]
    public void Validate_WhenNameAtMaxLength_Succeeds()
    {
        var request = ValidRequest();
        request.Name = new string('A', 200);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>UnitPrice negatif → hata.</summary>
    [Fact]
    public void Validate_WhenUnitPriceNegative_ReturnsError()
    {
        var request = ValidRequest();
        request.UnitPrice = -0.01m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.UnitPrice));
    }

    /// <summary>UnitPrice 0 → geçerli.</summary>
    [Fact]
    public void Validate_WhenUnitPriceZero_Succeeds()
    {
        var request = ValidRequest();
        request.UnitPrice = 0m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>SellingPrice negatif → hata.</summary>
    [Fact]
    public void Validate_WhenSellingPriceNegative_ReturnsError()
    {
        var request = ValidRequest();
        request.SellingPrice = -1m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.SellingPrice));
    }

    /// <summary>MinStockLevel negatif → hata.</summary>
    [Fact]
    public void Validate_WhenMinStockLevelNegative_ReturnsError()
    {
        var request = ValidRequest();
        request.MinStockLevel = -1;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.MinStockLevel));
    }

    /// <summary>Status enum dışı → hata.</summary>
    [Fact]
    public void Validate_WhenStatusOutOfEnum_ReturnsError()
    {
        var request = ValidRequest();
        request.Status = (ProductStatus)999;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Status));
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
