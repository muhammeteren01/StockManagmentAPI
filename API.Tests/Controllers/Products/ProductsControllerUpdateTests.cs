using API.Controllers;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Products;

/// <summary>ProductsController.Update birim testleri.</summary>
public class ProductsControllerUpdateTests
{
    private readonly Mock<IProductService> _productService = new();
    private readonly ProductsController _sut;

    public ProductsControllerUpdateTests()
    {
        _sut = new ProductsController(_productService.Object);
    }

    /// <summary>
    /// Update: servis başarılı ProductResponse → Ok(200);
    /// UpdateAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithProductResponse()
    {
        var id = Guid.NewGuid();
        var request = ProductsTestHelper.CreateUpdateRequest();
        var expected = ProductsTestHelper.CreateProductResponse(
            id,
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
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _productService.Verify(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Update: ürün yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = ProductsTestHelper.CreateUpdateRequest();
        _productService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Product bulunamadı: {id}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product bulunamadı: {id}");
    }

    /// <summary>
    /// Update: Name boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNameEmpty_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = ProductsTestHelper.CreateUpdateRequest(name: "");
        _productService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "'Name' must not be empty.")
            ]));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Update: başka üründe aynı SKU → InvalidOperationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenSkuAlreadyUsed_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var request = ProductsTestHelper.CreateUpdateRequest(sku: "DUP-SKU");
        _productService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");
    }
}
