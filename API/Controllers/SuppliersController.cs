using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Tedarikçi CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>Tüm tedarikçileri listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Supplier>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile tedarikçi getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Supplier>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    /// <summary>Şirkete ait tedarikçileri listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<IReadOnlyList<Supplier>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    /// <summary>Yeni tedarikçi oluşturur.</summary>
    [HttpPost]
    public async Task<ActionResult<Supplier>> Create([FromBody] Supplier supplier, CancellationToken cancellationToken)
    {
        var created = await _supplierService.CreateAsync(supplier, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Tedarikçiyi günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Supplier supplier, CancellationToken cancellationToken)
    {
        if (id != supplier.Id)
            return BadRequest("Id uyuşmuyor.");

        await _supplierService.UpdateAsync(supplier, cancellationToken);
        return NoContent();
    }

    /// <summary>Tedarikçiyi siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _supplierService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
