using System.Reflection;
using API.Controllers;
using Core.DTOs.Companies;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Companies;

/// <summary>CompaniesController.Create birim testleri.</summary>
public class CompaniesControllerCreateTests
{
    private readonly Mock<ICompanyService> _companyService = new();
    private readonly CompaniesController _sut;

    public CompaniesControllerCreateTests()
    {
        _sut = new CompaniesController(_companyService.Object);
    }

    /// <summary>
    /// Create: servis başarılı CompanyResponse döndüğünde CreatedAtAction(201),
    /// route GetById ve id route değeri doğru; CreateAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithCompanyResponse()
    {
        var request = CompaniesTestHelper.CreateCreateRequest();
        var expected = CompaniesTestHelper.CreateCompanyResponse(name: request.Name);
        _companyService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(CompaniesController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _companyService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// CreateCompanyRequest'te IsActive yok (Update'te bool vardır).
    /// JSON'da "IsActive": null gönderilse binder yok sayar; DTO'da null atanacak alan yok.
    /// Create yine de servise gider ve CreatedAtAction döner.
    /// </summary>
    [Fact]
    public async Task Create_WhenIsActiveNull_ReturnsCreatedAtActionBecauseDtoHasNoIsActive()
    {
        typeof(CreateCompanyRequest).GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance)
            .Should().BeNull("Create isteğinde IsActive yok; mapper entity'de IsActive=true atar");

        var request = CompaniesTestHelper.CreateCreateRequest();
        var expected = CompaniesTestHelper.CreateCompanyResponse(name: request.Name, isActive: true);
        _companyService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeEquivalentTo(expected);
        _companyService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
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
        var request = CompaniesTestHelper.CreateCreateRequest(name: name!);
        _companyService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "Şirket adı zorunludur.")
            ]));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
