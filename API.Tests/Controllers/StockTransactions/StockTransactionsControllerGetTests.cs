using API.Controllers;
using Core.DTOs.StockTransactions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.StockTransactions;

/// <summary>StockTransactionsController okuma endpoint'leri birim testleri.</summary>
public class StockTransactionsControllerGetTests
{
    private readonly Mock<IStockTransactionService> _stockTransactionService = new();
    private readonly StockTransactionsController _sut;

    public StockTransactionsControllerGetTests()
    {
        _sut = new StockTransactionsController(_stockTransactionService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<StockTransactionResponse>
        {
            StockTransactionsTestHelper.CreateTransactionResponse(quantity: 5),
            StockTransactionsTestHelper.CreateTransactionResponse(quantity: 8)
        };
        _stockTransactionService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _stockTransactionService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetAll: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _stockTransactionService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockTransactionResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<StockTransactionResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetById: hareket bulunduğunda Ok(200) ve StockTransactionResponse döner.
    /// </summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithTransactionResponse()
    {
        var id = Guid.NewGuid();
        var expected = StockTransactionsTestHelper.CreateTransactionResponse(id);
        _stockTransactionService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _stockTransactionService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetById: servis null → NotFound(404).
    /// </summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _stockTransactionService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransactionResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _stockTransactionService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByProduct: servis liste döndüğünde Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByProduct_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var productId = Guid.NewGuid();
        var expected = new List<StockTransactionResponse>
        {
            StockTransactionsTestHelper.CreateTransactionResponse(productId: productId)
        };
        _stockTransactionService
            .Setup(s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByProduct(productId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _stockTransactionService.Verify(
            s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByProduct: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetByProduct_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var productId = Guid.NewGuid();
        _stockTransactionService
            .Setup(s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockTransactionResponse>());

        var result = await _sut.GetByProduct(productId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<StockTransactionResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetByWarehouse: servis liste döndüğünde Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByWarehouse_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var warehouseId = Guid.NewGuid();
        var expected = new List<StockTransactionResponse>
        {
            StockTransactionsTestHelper.CreateTransactionResponse(warehouseId: warehouseId)
        };
        _stockTransactionService
            .Setup(s => s.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByWarehouse(warehouseId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _stockTransactionService.Verify(
            s => s.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByWarehouse: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetByWarehouse_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var warehouseId = Guid.NewGuid();
        _stockTransactionService
            .Setup(s => s.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockTransactionResponse>());

        var result = await _sut.GetByWarehouse(warehouseId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<StockTransactionResponse>>()
            .Which.Should().BeEmpty();
    }
}
