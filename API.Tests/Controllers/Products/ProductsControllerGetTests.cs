using API.Controllers;
using Core.DTOs.Products;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Products;

/// <summary>ProductsController okuma endpoint'leri birim testleri.</summary>
public class ProductsControllerGetTests
{
    private readonly Mock<IProductService> _productService = new();
    private readonly ProductsController _sut;

    public ProductsControllerGetTests()
    {
        _sut = new ProductsController(_productService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<ProductResponse>
        {
            ProductsTestHelper.CreateProductResponse(name: "Alpha", sku: "A-1"),
            ProductsTestHelper.CreateProductResponse(name: "Beta", sku: "B-2")
        };
        _productService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _productService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetAll: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _productService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<ProductResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetById: ürün bulunduğunda Ok(200) ve ProductResponse döner.
    /// </summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithProductResponse()
    {
        var id = Guid.NewGuid();
        var expected = ProductsTestHelper.CreateProductResponse(id);
        _productService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _productService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetById: servis null → NotFound(404).
    /// </summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _productService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _productService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: servis liste döndüğünde Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var companyId = Guid.NewGuid();
        var expected = new List<ProductResponse>
        {
            ProductsTestHelper.CreateProductResponse(companyId: companyId)
        };
        _productService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _productService.Verify(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var companyId = Guid.NewGuid();
        _productService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductResponse>());

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<ProductResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetByCompany: tenant erişim yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenAccessDenied_ThrowsForbiddenException()
    {
        var companyId = Guid.NewGuid();
        _productService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.GetByCompany(companyId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }
}
