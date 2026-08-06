using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Warehouses;

/// <summary>WarehousesController.Create birim testleri.</summary>
public class WarehousesControllerCreateTests
{
    private readonly Mock<IWarehouseService> _warehouseService = new();
    private readonly WarehousesController _sut;

    public WarehousesControllerCreateTests()
    {
        _sut = new WarehousesController(_warehouseService.Object);
    }

    /// <summary>
    /// Create: servis başarılı WarehouseResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithWarehouseResponse()
    {
        var request = WarehousesTestHelper.CreateCreateRequest();
        var expected = WarehousesTestHelper.CreateWarehouseResponse(
            companyId: request.CompanyId,
            name: request.Name,
            location: request.Location,
            capacity: request.Capacity);
        _warehouseService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(WarehousesController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _warehouseService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
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
        var request = WarehousesTestHelper.CreateCreateRequest(name: name!);
        _warehouseService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "Depo adı zorunludur.")
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
        var request = WarehousesTestHelper.CreateCreateRequest(companyId: null);
        request.CompanyId = null;
        _warehouseService
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
        var request = WarehousesTestHelper.CreateCreateRequest();
        _warehouseService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Şirket bilgisi bulunamadı."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }
}
