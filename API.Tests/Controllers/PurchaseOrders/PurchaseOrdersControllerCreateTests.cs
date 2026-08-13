using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.PurchaseOrders;

/// <summary>PurchaseOrdersController.Create birim testleri.</summary>
public class PurchaseOrdersControllerCreateTests
{
    private readonly Mock<IPurchaseOrderService> _purchaseOrderService = new();
    private readonly PurchaseOrdersController _sut;

    public PurchaseOrdersControllerCreateTests()
    {
        _sut = PurchaseOrdersControllerTestHelper.CreateSut(_purchaseOrderService);
    }

    /// <summary>
    /// Create: servis başarılı PurchaseOrderResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithOrderResponse()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        var expected = PurchaseOrdersTestHelper.CreateOrderResponse(
            supplierId: request.SupplierId,
            warehouseId: request.WarehouseId,
            orderNumber: request.OrderNumber,
            itemQuantity: request.Items[0].Quantity,
            unitPrice: request.Items[0].UnitPrice,
            totalAmount: request.Items[0].Quantity * request.Items[0].UnitPrice);
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(PurchaseOrdersController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _purchaseOrderService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Create: kalem Quantity ≤ 0 → ValidationException iletilir.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WhenItemQuantityNotPositive_ThrowsValidationException(int quantity)
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest(quantity: quantity);
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("Items[0].Quantity", "'Quantity' must be greater than '0'.")
            ]));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Create: tedarikçi yok → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenSupplierNotFound_ThrowsInvalidOperationException()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Tedarikçi bulunamadı: {request.SupplierId}"));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Tedarikçi bulunamadı: {request.SupplierId}");
    }

    /// <summary>Create: tedarikçi farklı şirket → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenSupplierDifferentCompany_ThrowsInvalidOperationException()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tedarikçi farklı bir şirkete ait."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tedarikçi farklı bir şirkete ait.");
    }

    /// <summary>Create: depo farklı şirket → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenWarehouseDifferentCompany_ThrowsInvalidOperationException()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Depo farklı bir şirkete ait."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Depo farklı bir şirkete ait.");
    }

    /// <summary>Create: ürün farklı şirket → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Create_WhenProductDifferentCompany_ThrowsInvalidOperationException()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                $"Ürün farklı bir şirkete ait: {request.Items[0].ProductId}"));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün farklı bir şirkete ait: {request.Items[0].ProductId}");
    }

    /// <summary>Create: şirket erişimi yok → ForbiddenException iletilir.</summary>
    [Fact]
    public async Task Create_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Şirket bilgisi bulunamadı."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Şirket bilgisi bulunamadı.");
    }

    /// <summary>Create: eşzamanlılık çakışması → ConflictException iletilir.</summary>
    [Fact]
    public async Task Create_WhenConcurrencyConflict_ThrowsConflictException()
    {
        var request = PurchaseOrdersTestHelper.CreateCreateRequest();
        _purchaseOrderService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
    }
}
