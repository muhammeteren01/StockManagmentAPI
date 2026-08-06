using API.Controllers;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Suppliers;

/// <summary>SuppliersController.Update birim testleri.</summary>
public class SuppliersControllerUpdateTests
{
    private readonly Mock<ISupplierService> _supplierService = new();
    private readonly SuppliersController _sut;

    public SuppliersControllerUpdateTests()
    {
        _sut = new SuppliersController(_supplierService.Object);
    }

    /// <summary>
    /// Update: servis başarılı SupplierResponse → Ok(200);
    /// UpdateAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithSupplierResponse()
    {
        var id = Guid.NewGuid();
        var request = SuppliersTestHelper.CreateUpdateRequest();
        var expected = SuppliersTestHelper.CreateSupplierResponse(
            id,
            companyName: request.CompanyName,
            contactName: request.ContactName,
            phone: request.Phone,
            email: request.Email,
            address: request.Address,
            taxNumber: request.TaxNumber);
        _supplierService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _supplierService.Verify(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Update: tedarikçi yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = SuppliersTestHelper.CreateUpdateRequest();
        _supplierService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Supplier bulunamadı: {id}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Supplier bulunamadı: {id}");
    }

    /// <summary>
    /// Update: CompanyName boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenCompanyNameEmpty_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = SuppliersTestHelper.CreateUpdateRequest(companyName: "");
        _supplierService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.CompanyName), "'Company Name' must not be empty.")
            ]));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
