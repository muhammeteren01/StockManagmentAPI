using API.Controllers;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Categories;

/// <summary>CategoriesController.Update birim testleri.</summary>
public class CategoriesControllerUpdateTests
{
    private readonly Mock<ICategoryService> _categoryService = new();
    private readonly CategoriesController _sut;

    public CategoriesControllerUpdateTests()
    {
        _sut = new CategoriesController(_categoryService.Object);
    }

    /// <summary>
    /// Update: servis başarılı CategoryResponse → Ok(200);
    /// UpdateAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithCategoryResponse()
    {
        var id = Guid.NewGuid();
        var request = CategoriesTestHelper.CreateUpdateRequest();
        var expected = CategoriesTestHelper.CreateCategoryResponse(
            id,
            name: request.Name,
            description: request.Description);
        _categoryService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _categoryService.Verify(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Update: kategori yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = CategoriesTestHelper.CreateUpdateRequest();
        _categoryService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Category bulunamadı: {id}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Category bulunamadı: {id}");
    }

    /// <summary>
    /// Update: Name boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNameEmpty_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = CategoriesTestHelper.CreateUpdateRequest(name: "");
        _categoryService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "'Name' must not be empty.")
            ]));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
