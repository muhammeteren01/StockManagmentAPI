using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Products;

/// <summary>ProductsController.Create birim testleri.</summary>
public class ProductsControllerCreateTests
{
    private readonly Mock<IProductService> _productService = new();
    private readonly ProductsController _sut;

    public ProductsControllerCreateTests()
    {
        _sut = new ProductsController(_productService.Object);
    }

    /// <summary>
    /// Create: servis başarılı ProductResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithProductResponse()
    {
        var request = ProductsTestHelper.CreateCreateRequest();
        var expected = ProductsTestHelper.CreateProductResponse(
            companyId: request.CompanyId,
            categoryId: request.CategoryId,
            supplierId: request.SupplierId,
            sku: request.Sku,
            barcode: request.Barcode,
            name: request.Name,
            description: request.Description,
            unitPrice: request.UnitPrice,
            sellingPrice: request.SellingPrice,
            minStockLevel: request.MinStockLevel,
            status: request.Status);
        _productService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(ProductsController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _productService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Create: Name boş → ValidationException iletilir (controller yakalamaz).
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WhenNameEmptyOrNull_ThrowsValidationException(string? name)
    {
        var request = ProductsTestHelper.CreateCreateRequest(name: name!);
        _productService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "'Name' must not be empty.")
            ]));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Create: SuperAdmin CompanyId göndermedi → InvalidOperationException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenSuperAdminMissingCompanyId_ThrowsInvalidOperationException()
    {
        var request = ProductsTestHelper.CreateCreateRequest(companyId: null);
        request.CompanyId = null;
        _productService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SuperAdmin için CompanyId zorunludur."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SuperAdmin için CompanyId zorunludur.");
    }

    /// <summary>
    /// Create: token'da şirket yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenCompanyMissing_ThrowsForbiddenException()
    {
        var request = ProductsTestHelper.CreateCreateRequest();
        _productService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Şirket bilgisi bulunamadı."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }

    /// <summary>
    /// Create: aynı şirkette SKU kullanılıyor → InvalidOperationException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenSkuAlreadyUsed_ThrowsInvalidOperationException()
    {
        var request = ProductsTestHelper.CreateCreateRequest(sku: "DUP-SKU");
        _productService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}"));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");
    }
}
