using API.Controllers;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Companies;

/// <summary>CompaniesController.Update birim testleri.</summary>
public class CompaniesControllerUpdateTests
{
    private readonly Mock<ICompanyService> _companyService = new();
    private readonly CompaniesController _sut;

    public CompaniesControllerUpdateTests()
    {
        _sut = new CompaniesController(_companyService.Object);
    }

    /// <summary>
    /// Update: servis başarılı CompanyResponse döndüğünde Ok(200);
    /// UpdateAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithCompanyResponse()
    {
        var id = Guid.NewGuid();
        var request = CompaniesTestHelper.CreateUpdateRequest();
        var expected = CompaniesTestHelper.CreateCompanyResponse(id, request.Name, request.IsActive);
        _companyService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _companyService.Verify(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Update: şirket yok → KeyNotFoundException iletilir (servis davranışı).
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = CompaniesTestHelper.CreateUpdateRequest();
        _companyService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Company bulunamadı: {id}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Company bulunamadı: {id}");
    }

    /// <summary>
    /// Update: Name boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNameEmpty_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = CompaniesTestHelper.CreateUpdateRequest(name: "");
        _companyService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "Şirket adı zorunludur.")
            ]));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
