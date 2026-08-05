using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Depo CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>Tüm depoları listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Warehouse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile depo getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Warehouse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, cancellationToken);
        return warehouse is null ? NotFound() : Ok(warehouse);
    }

    /// <summary>Şirkete ait depoları listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<IReadOnlyList<Warehouse>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    /// <summary>Yeni depo oluşturur.</summary>
    [HttpPost]
    public async Task<ActionResult<Warehouse>> Create([FromBody] Warehouse warehouse, CancellationToken cancellationToken)
    {
        var created = await _warehouseService.CreateAsync(warehouse, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Depoyu günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Warehouse warehouse, CancellationToken cancellationToken)
    {
        if (id != warehouse.Id)
            return BadRequest("Id uyuşmuyor.");

        await _warehouseService.UpdateAsync(warehouse, cancellationToken);
        return NoContent();
    }

    /// <summary>Depoyu siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
