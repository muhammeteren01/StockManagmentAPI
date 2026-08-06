using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Categories;

/// <summary>CategoriesController.Create birim testleri.</summary>
public class CategoriesControllerCreateTests
{
    private readonly Mock<ICategoryService> _categoryService = new();
    private readonly CategoriesController _sut;

    public CategoriesControllerCreateTests()
    {
        _sut = new CategoriesController(_categoryService.Object);
    }

    /// <summary>
    /// Create: servis başarılı CategoryResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithCategoryResponse()
    {
        var request = CategoriesTestHelper.CreateCreateRequest();
        var expected = CategoriesTestHelper.CreateCategoryResponse(
            companyId: request.CompanyId,
            name: request.Name,
            description: request.Description);
        _categoryService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(CategoriesController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _categoryService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
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
        var request = CategoriesTestHelper.CreateCreateRequest(name: name!);
        _categoryService
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
        var request = CategoriesTestHelper.CreateCreateRequest(companyId: null);
        request.CompanyId = null;
        _categoryService
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
        var request = CategoriesTestHelper.CreateCreateRequest();
        _categoryService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Şirket bilgisi bulunamadı."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }
}
