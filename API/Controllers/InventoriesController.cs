using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Stok (inventory) sorgulama endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoriesController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoriesController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>Tüm stok satırlarını listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Inventory>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile stok satırı getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Inventory>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetByIdAsync(id, cancellationToken);
        return inventory is null ? NotFound() : Ok(inventory);
    }

    /// <summary>Ürün + depo çiftine göre stok getirir.</summary>
    [HttpGet("by-product/{productId:guid}/warehouse/{warehouseId:guid}")]
    public async Task<ActionResult<Inventory>> GetByProductAndWarehouse(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        return inventory is null ? NotFound() : Ok(inventory);
    }

    /// <summary>Depodaki stokları listeler.</summary>
    [HttpGet("by-warehouse/{warehouseId:guid}")]
    public async Task<ActionResult<IReadOnlyList<Inventory>>> GetByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.GetByWarehouseIdAsync(warehouseId, cancellationToken));
    }

    /// <summary>Ürünün tüm depolardaki stoklarını listeler.</summary>
    [HttpGet("by-product/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<Inventory>>> GetByProduct(Guid productId, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.GetByProductIdAsync(productId, cancellationToken));
    }
}
