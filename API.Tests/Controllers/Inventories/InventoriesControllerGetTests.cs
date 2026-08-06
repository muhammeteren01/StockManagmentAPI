using API.Controllers;
using Core.DTOs.Inventories;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Inventories;

/// <summary>InventoriesController okuma endpoint'leri birim testleri.</summary>
public class InventoriesControllerGetTests
{
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly InventoriesController _sut;

    public InventoriesControllerGetTests()
    {
        _sut = new InventoriesController(_inventoryService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<InventoryResponse>
        {
            InventoriesTestHelper.CreateInventoryResponse(quantity: 5),
            InventoriesTestHelper.CreateInventoryResponse(quantity: 20)
        };
        _inventoryService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _inventoryService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetAll: boş liste Ok(200) ve boş dizi.</summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _inventoryService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InventoryResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<InventoryResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>GetById: stok bulunduğunda Ok(200) ve InventoryResponse döner.</summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithInventoryResponse()
    {
        var id = Guid.NewGuid();
        var expected = InventoriesTestHelper.CreateInventoryResponse(id);
        _inventoryService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _inventoryService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetById: servis null → NotFound(404).</summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _inventoryService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _inventoryService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByProductAndWarehouse: bulunduğunda Ok(200).</summary>
    [Fact]
    public async Task GetByProductAndWarehouse_WhenFound_ReturnsOkWithInventoryResponse()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var expected = InventoriesTestHelper.CreateInventoryResponse(productId: productId, warehouseId: warehouseId);
        _inventoryService
            .Setup(s => s.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByProductAndWarehouse(productId, warehouseId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _inventoryService.Verify(
            s => s.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>GetByProductAndWarehouse: servis null → NotFound(404).</summary>
    [Fact]
    public async Task GetByProductAndWarehouse_WhenNotFound_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        _inventoryService
            .Setup(s => s.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryResponse?)null);

        var result = await _sut.GetByProductAndWarehouse(productId, warehouseId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>GetByWarehouse: servis liste döndüğünde Ok(200).</summary>
    [Fact]
    public async Task GetByWarehouse_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var warehouseId = Guid.NewGuid();
        var expected = new List<InventoryResponse>
        {
            InventoriesTestHelper.CreateInventoryResponse(warehouseId: warehouseId)
        };
        _inventoryService
            .Setup(s => s.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByWarehouse(warehouseId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _inventoryService.Verify(s => s.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByWarehouse: boş liste Ok(200).</summary>
    [Fact]
    public async Task GetByWarehouse_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var warehouseId = Guid.NewGuid();
        _inventoryService
            .Setup(s => s.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InventoryResponse>());

        var result = await _sut.GetByWarehouse(warehouseId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<InventoryResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>GetByProduct: servis liste döndüğünde Ok(200).</summary>
    [Fact]
    public async Task GetByProduct_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var productId = Guid.NewGuid();
        var expected = new List<InventoryResponse>
        {
            InventoriesTestHelper.CreateInventoryResponse(productId: productId)
        };
        _inventoryService
            .Setup(s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByProduct(productId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _inventoryService.Verify(s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByProduct: boş liste Ok(200).</summary>
    [Fact]
    public async Task GetByProduct_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var productId = Guid.NewGuid();
        _inventoryService
            .Setup(s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InventoryResponse>());

        var result = await _sut.GetByProduct(productId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<InventoryResponse>>()
            .Which.Should().BeEmpty();
    }
}
