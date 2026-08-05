using Core.Authorization;
using Core.DTOs.Warehouses;
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

    public WarehousesController(IWarehouseService warehouseService) => _warehouseService = warehouseService;

    /// <summary>Tüm depoları listeler.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<WarehouseResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _warehouseService.GetAllAsync(cancellationToken));

    /// <summary>Id ile depo getirir.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<WarehouseResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, cancellationToken);
        return warehouse is null ? NotFound() : Ok(warehouse);
    }

    /// <summary>Şirkete ait depoları listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<WarehouseResponse>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
        => Ok(await _warehouseService.GetByCompanyIdAsync(companyId, cancellationToken));

    /// <summary>Yeni depo oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<WarehouseResponse>> Create([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var created = await _warehouseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Depoyu günceller.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<WarehouseResponse>> Update(Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken)
        => Ok(await _warehouseService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Depoyu siler.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
