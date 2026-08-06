using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.StockTransfers;

/// <summary>StockTransfersController.Create birim testleri.</summary>
public class StockTransfersControllerCreateTests
{
    private readonly Mock<IStockTransferService> _stockTransferService = new();
    private readonly StockTransfersController _sut;

    public StockTransfersControllerCreateTests()
    {
        _sut = new StockTransfersController(_stockTransferService.Object);
    }

    /// <summary>
    /// Create: servis başarılı StockTransferResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithTransferResponse()
    {
        var request = StockTransfersTestHelper.CreateCreateRequest();
        var expected = StockTransfersTestHelper.CreateTransferResponse(
            fromWarehouseId: request.FromWarehouseId,
            toWarehouseId: request.ToWarehouseId,
            referenceNo: request.ReferenceNo,
            notes: request.Notes,
            itemQuantity: request.Items[0].Quantity);
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(StockTransfersController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _stockTransferService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: kalem Quantity ≤ 0 → ValidationException iletilir.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WhenItemQuantityNotPositive_ThrowsValidationException(int quantity)
    {
        var request = StockTransfersTestHelper.CreateCreateRequest(quantity: quantity);
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("Items[0].Quantity", "'Quantity' must be greater than '0'.")
            ]));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Create: kaynak = hedef depo → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenFromAndToSameWarehouse_ThrowsInvalidOperationException()
    {
        var warehouseId = Guid.NewGuid();
        var request = StockTransfersTestHelper.CreateCreateRequest(
            fromWarehouseId: warehouseId, toWarehouseId: warehouseId);
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kaynak ve hedef depo farklı olmalıdır."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Kaynak ve hedef depo farklı olmalıdır.");
    }

    /// <summary>Create: kaynak depo yok → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenFromWarehouseNotFound_ThrowsInvalidOperationException()
    {
        var request = StockTransfersTestHelper.CreateCreateRequest();
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Kaynak depo bulunamadı: {request.FromWarehouseId}"));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Kaynak depo bulunamadı: {request.FromWarehouseId}");
    }

    /// <summary>Create: depolar farklı şirket → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenWarehousesDifferentCompany_ThrowsInvalidOperationException()
    {
        var request = StockTransfersTestHelper.CreateCreateRequest();
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kaynak ve hedef depolar aynı şirkete ait olmalıdır."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Kaynak ve hedef depolar aynı şirkete ait olmalıdır.");
    }

    /// <summary>Create: şirket erişimi yok → ForbiddenException iletilir.</summary>
    [Fact]
    public async Task Create_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var request = StockTransfersTestHelper.CreateCreateRequest();
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }

    /// <summary>Create: eşzamanlılık çakışması → ConflictException iletilir.</summary>
    [Fact]
    public async Task Create_WhenConcurrencyConflict_ThrowsConflictException()
    {
        var request = StockTransfersTestHelper.CreateCreateRequest();
        _stockTransferService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
    }
}
