using API.Controllers;
using Core.DTOs.Categories;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Categories;

/// <summary>CategoriesController okuma endpoint'leri birim testleri.</summary>
public class CategoriesControllerGetTests
{
    private readonly Mock<ICategoryService> _categoryService = new();
    private readonly CategoriesController _sut;

    public CategoriesControllerGetTests()
    {
        _sut = new CategoriesController(_categoryService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<CategoryResponse>
        {
            CategoriesTestHelper.CreateCategoryResponse(name: "Alpha"),
            CategoriesTestHelper.CreateCategoryResponse(name: "Beta")
        };
        _categoryService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _categoryService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetAll: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _categoryService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CategoryResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<CategoryResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetById: kategori bulunduğunda Ok(200) ve CategoryResponse döner.
    /// </summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithCategoryResponse()
    {
        var id = Guid.NewGuid();
        var expected = CategoriesTestHelper.CreateCategoryResponse(id);
        _categoryService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _categoryService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetById: servis null → NotFound(404).
    /// </summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _categoryService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _categoryService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: servis liste döndüğünde Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var companyId = Guid.NewGuid();
        var expected = new List<CategoryResponse>
        {
            CategoriesTestHelper.CreateCategoryResponse(companyId: companyId)
        };
        _categoryService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _categoryService.Verify(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var companyId = Guid.NewGuid();
        _categoryService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CategoryResponse>());

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<CategoryResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetByCompany: tenant erişim yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenAccessDenied_ThrowsForbiddenException()
    {
        var companyId = Guid.NewGuid();
        _categoryService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.GetByCompany(companyId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }
}
