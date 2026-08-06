using Core.Entities;
using Core.Repositories;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Inventories;

/// <summary>
/// InventoryService sorgu birim testleri.
/// Inventory API salt okunur; Create/Update/ConflictException/same-company burada yok
/// (stok yazımı StockTransaction / Transfer / PurchaseOrder tarafında).
/// </summary>
public class InventoryServiceGetTests
{
    private readonly Mock<IInventoryRepository> _repository = new();
    private readonly InventoryService _sut;

    public InventoryServiceGetTests()
    {
        _sut = InventoryServiceTestHelper.CreateSut(_repository);
    }

    /// <summary>GetById: entity yok → null.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
        _repository.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetById: entity var → DTO alanları map edilir (RowVersion response'ta yok).</summary>
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var entity = InventoryServiceTestHelper.CreateEntity(quantity: 42);
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.CompanyId.Should().Be(entity.CompanyId);
        result.ProductId.Should().Be(entity.ProductId);
        result.WarehouseId.Should().Be(entity.WarehouseId);
        result.Quantity.Should().Be(42);
        result.LastUpdated.Should().Be(entity.LastUpdated);
    }

    /// <summary>GetAll: entity listesi → mapped response listesi.</summary>
    [Fact]
    public async Task GetAllAsync_WhenItemsExist_ReturnsMappedList()
    {
        var entities = new List<Inventory>
        {
            InventoryServiceTestHelper.CreateEntity(quantity: 1),
            InventoryServiceTestHelper.CreateEntity(quantity: 2)
        };
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(entities[0].Id);
        result[0].Quantity.Should().Be(1);
        result[1].Id.Should().Be(entities[1].Id);
        result[1].Quantity.Should().Be(2);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetAll: boş liste → boş response.</summary>
    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Inventory>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    /// <summary>GetByProductAndWarehouse: bulunamaz → null.</summary>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_WhenNotFound_ReturnsNull()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var result = await _sut.GetByProductAndWarehouseAsync(productId, warehouseId);

        result.Should().BeNull();
    }

    /// <summary>GetByProductAndWarehouse: bulunur → mapped response.</summary>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_WhenFound_ReturnsMappedResponse()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var entity = InventoryServiceTestHelper.CreateEntity(productId: productId, warehouseId: warehouseId, quantity: 7);
        _repository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _sut.GetByProductAndWarehouseAsync(productId, warehouseId);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.WarehouseId.Should().Be(warehouseId);
        result.Quantity.Should().Be(7);
    }

    /// <summary>GetByWarehouseId: depo stokları map edilir.</summary>
    [Fact]
    public async Task GetByWarehouseIdAsync_WhenItemsExist_ReturnsMappedList()
    {
        var warehouseId = Guid.NewGuid();
        var entities = new List<Inventory>
        {
            InventoryServiceTestHelper.CreateEntity(warehouseId: warehouseId, quantity: 3)
        };
        _repository
            .Setup(r => r.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetByWarehouseIdAsync(warehouseId);

        result.Should().ContainSingle();
        result[0].WarehouseId.Should().Be(warehouseId);
        result[0].Quantity.Should().Be(3);
        _repository.Verify(r => r.GetByWarehouseIdAsync(warehouseId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByProductId: ürünün tüm depo stokları map edilir.</summary>
    [Fact]
    public async Task GetByProductIdAsync_WhenItemsExist_ReturnsMappedList()
    {
        var productId = Guid.NewGuid();
        var entities = new List<Inventory>
        {
            InventoryServiceTestHelper.CreateEntity(productId: productId, quantity: 9),
            InventoryServiceTestHelper.CreateEntity(productId: productId, quantity: 0)
        };
        _repository
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _sut.GetByProductIdAsync(productId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.ProductId == productId);
        result[1].Quantity.Should().Be(0);
        _repository.Verify(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
