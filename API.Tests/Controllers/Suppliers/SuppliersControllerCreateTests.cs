using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Suppliers;

/// <summary>SuppliersController.Create birim testleri.</summary>
public class SuppliersControllerCreateTests
{
    private readonly Mock<ISupplierService> _supplierService = new();
    private readonly SuppliersController _sut;

    public SuppliersControllerCreateTests()
    {
        _sut = new SuppliersController(_supplierService.Object);
    }

    /// <summary>
    /// Create: servis başarılı SupplierResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithSupplierResponse()
    {
        var request = SuppliersTestHelper.CreateCreateRequest();
        var expected = SuppliersTestHelper.CreateSupplierResponse(
            companyId: request.CompanyId,
            companyName: request.CompanyName,
            contactName: request.ContactName,
            phone: request.Phone,
            email: request.Email,
            address: request.Address,
            taxNumber: request.TaxNumber);
        _supplierService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(SuppliersController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _supplierService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Create: CompanyName boş → ValidationException iletilir (controller yakalamaz).
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WhenCompanyNameEmptyOrNull_ThrowsValidationException(string? companyName)
    {
        var request = SuppliersTestHelper.CreateCreateRequest(companyName: companyName!);
        _supplierService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.CompanyName), "'Company Name' must not be empty.")
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
        var request = SuppliersTestHelper.CreateCreateRequest(companyId: null);
        request.CompanyId = null;
        _supplierService
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
        var request = SuppliersTestHelper.CreateCreateRequest();
        _supplierService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Şirket bilgisi bulunamadı."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }
}
