using API.Controllers;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.StockTransfers;

/// <summary>StockTransfersController Start / Complete / Cancel birim testleri.</summary>
public class StockTransfersControllerLifecycleTests
{
    private readonly Mock<IStockTransferService> _stockTransferService = new();
    private readonly StockTransfersController _sut;

    public StockTransfersControllerLifecycleTests()
    {
        _sut = new StockTransfersController(_stockTransferService.Object);
    }

    /// <summary>Start: başarılı → NoContent(204); StartAsync bir kez.</summary>
    [Fact]
    public async Task Start_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.StartAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Start(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _stockTransferService.Verify(s => s.StartAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Start: transfer yok → KeyNotFoundException iletilir.</summary>
    [Fact]
    public async Task Start_WhenTransferNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.StartAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Transfer bulunamadı: {id}"));

        var act = async () => await _sut.Start(id, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Transfer bulunamadı: {id}");
    }

    /// <summary>Start: Pending değil → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Start_WhenNotPending_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.StartAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sadece Pending transferler başlatılabilir."));

        var act = async () => await _sut.Start(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece Pending transferler başlatılabilir.");
    }

    /// <summary>Start: yetersiz stok → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Start_WhenInsufficientStock_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.StartAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Yetersiz stok."));

        var act = async () => await _sut.Start(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Yetersiz stok.");
    }

    /// <summary>Complete: başarılı → NoContent(204).</summary>
    [Fact]
    public async Task Complete_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.CompleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Complete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _stockTransferService.Verify(s => s.CompleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Complete: InTransit değil → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Complete_WhenNotInTransit_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.CompleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sadece InTransit transferler tamamlanabilir."));

        var act = async () => await _sut.Complete(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sadece InTransit transferler tamamlanabilir.");
    }

    /// <summary>Complete: concurrency → ConflictException iletilir.</summary>
    [Fact]
    public async Task Complete_WhenConcurrencyConflict_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.CompleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.Complete(id, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Cancel: başarılı → NoContent(204).</summary>
    [Fact]
    public async Task Cancel_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Cancel(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _stockTransferService.Verify(s => s.CancelAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Cancel: Completed/Cancelled → InvalidOperationException iletilir.</summary>
    [Fact]
    public async Task Cancel_WhenAlreadyCompletedOrCancelled_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Tamamlanmış veya iptal edilmiş transfer iptal edilemez."));

        var act = async () => await _sut.Cancel(id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tamamlanmış veya iptal edilmiş transfer iptal edilemez.");
    }
}
