using API.Controllers;
using Core.DTOs.StockTransfers;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.StockTransfers;

/// <summary>StockTransfersController okuma endpoint'leri birim testleri.</summary>
public class StockTransfersControllerGetTests
{
    private readonly Mock<IStockTransferService> _stockTransferService = new();
    private readonly StockTransfersController _sut;

    public StockTransfersControllerGetTests()
    {
        _sut = new StockTransfersController(_stockTransferService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<StockTransferResponse>
        {
            StockTransfersTestHelper.CreateTransferResponse(itemQuantity: 5),
            StockTransfersTestHelper.CreateTransferResponse(itemQuantity: 8)
        };
        _stockTransferService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _stockTransferService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetAll: boş liste Ok(200) ve boş dizi.</summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _stockTransferService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockTransferResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<StockTransferResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>GetById: transfer bulunduğunda Ok(200) ve StockTransferResponse döner.</summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithTransferResponse()
    {
        var id = Guid.NewGuid();
        var expected = StockTransfersTestHelper.CreateTransferResponse(id);
        _stockTransferService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _stockTransferService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetById: servis null → NotFound(404).</summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _stockTransferService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransferResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _stockTransferService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
