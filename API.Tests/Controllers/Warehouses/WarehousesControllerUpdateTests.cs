using API.Controllers;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Warehouses;

/// <summary>WarehousesController.Update birim testleri.</summary>
public class WarehousesControllerUpdateTests
{
    private readonly Mock<IWarehouseService> _warehouseService = new();
    private readonly WarehousesController _sut;

    public WarehousesControllerUpdateTests()
    {
        _sut = new WarehousesController(_warehouseService.Object);
    }

    /// <summary>
    /// Update: servis başarılı WarehouseResponse → Ok(200);
    /// UpdateAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithWarehouseResponse()
    {
        var id = Guid.NewGuid();
        var request = WarehousesTestHelper.CreateUpdateRequest();
        var expected = WarehousesTestHelper.CreateWarehouseResponse(
            id,
            name: request.Name,
            location: request.Location,
            capacity: request.Capacity,
            isActive: request.IsActive);
        _warehouseService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _warehouseService.Verify(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Update: depo yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = WarehousesTestHelper.CreateUpdateRequest();
        _warehouseService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Warehouse bulunamadı: {id}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Warehouse bulunamadı: {id}");
    }

    /// <summary>
    /// Update: Name boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNameEmpty_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = WarehousesTestHelper.CreateUpdateRequest(name: "");
        _warehouseService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "Depo adı zorunludur.")
            ]));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
