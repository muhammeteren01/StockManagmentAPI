using API.Controllers;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.PurchaseOrders;

/// <summary>PurchaseOrdersController Approve / Receive / Cancel birim testleri.</summary>
public class PurchaseOrdersControllerLifecycleTests
{
    private readonly Mock<IPurchaseOrderService> _purchaseOrderService = new();
    private readonly PurchaseOrdersController _sut;

    public PurchaseOrdersControllerLifecycleTests()
    {
        _sut = PurchaseOrdersControllerTestHelper.CreateSut(_purchaseOrderService);
    }

    /// <summary>Approve: başarılı → NoContent(204); ApproveAsync bir kez.</summary>
    [Fact]
    public async Task Approve_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.ApproveAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Approve(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _purchaseOrderService.Verify(s => s.ApproveAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Approve: sipariş yok → KeyNotFoundException iletilir.</summary>
    [Fact]
    public async Task Approve_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.ApproveAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Sipariş bulunamadı: {id}"));

        var act = async () => await _sut.Approve(id, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sipariş bulunamadı: {id}");
    }

    /// <summary>Approve: Pending değil → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Approve_WhenNotPending_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.ApproveAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sadece Pending siparişler onaylanabilir."));

        var act = async () => await _sut.Approve(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece Pending siparişler onaylanabilir.");
    }

    /// <summary>Receive: başarılı → NoContent(204); ReceivedQuantities servise iletilir.</summary>
    [Fact]
    public async Task Receive_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var request = PurchaseOrdersTestHelper.CreateReceiveRequest(quantity: 4);
        _purchaseOrderService
            .Setup(s => s.ReceiveAsync(id, request.ReceivedQuantities, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Receive(id, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _purchaseOrderService.Verify(
            s => s.ReceiveAsync(id, request.ReceivedQuantities, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>Receive: sipariş yok → KeyNotFoundException iletilir.</summary>
    [Fact]
    public async Task Receive_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = PurchaseOrdersTestHelper.CreateReceiveRequest();
        _purchaseOrderService
            .Setup(s => s.ReceiveAsync(id, request.ReceivedQuantities, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Sipariş bulunamadı: {id}"));

        var act = async () => await _sut.Receive(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Sipariş bulunamadı: {id}");
    }

    /// <summary>Receive: Approved/Received değil → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Receive_WhenStatusNotReceivable_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var request = PurchaseOrdersTestHelper.CreateReceiveRequest();
        _purchaseOrderService
            .Setup(s => s.ReceiveAsync(id, request.ReceivedQuantities, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Sadece Approved veya kısmen Received siparişlerde mal kabulü yapılabilir."));

        var act = async () => await _sut.Receive(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece Approved veya kısmen Received siparişlerde mal kabulü yapılabilir.");
    }

    /// <summary>Receive: fazla kabul miktarı → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Receive_WhenOverReceivedQuantity_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = PurchaseOrdersTestHelper.CreateReceiveRequest(productId, quantity: 99);
        _purchaseOrderService
            .Setup(s => s.ReceiveAsync(id, request.ReceivedQuantities, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Ürün için fazla kabul miktarı: {productId}"));

        var act = async () => await _sut.Receive(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün için fazla kabul miktarı: {productId}");
    }

    /// <summary>Receive: concurrency → ConflictException iletilir.</summary>
    [Fact]
    public async Task Receive_WhenConcurrencyConflict_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var request = PurchaseOrdersTestHelper.CreateReceiveRequest();
        _purchaseOrderService
            .Setup(s => s.ReceiveAsync(id, request.ReceivedQuantities, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.Receive(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Cancel: başarılı → NoContent(204).</summary>
    [Fact]
    public async Task Cancel_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Cancel(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _purchaseOrderService.Verify(s => s.CancelAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Cancel: Received/Cancelled → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Cancel_WhenAlreadyReceivedOrCancelled_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Teslim alınmış veya iptal edilmiş sipariş iptal edilemez."));

        var act = async () => await _sut.Cancel(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Teslim alınmış veya iptal edilmiş sipariş iptal edilemez.");
    }

    /// <summary>Cancel: kısmi mal kabulü var → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Cancel_WhenPartialReceiveExists_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Kısmi mal kabulü yapılmış sipariş iptal edilemez."));

        var act = async () => await _sut.Cancel(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Kısmi mal kabulü yapılmış sipariş iptal edilemez.");
    }
}
