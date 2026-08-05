using Core.Authorization;
using Core.DTOs.Suppliers;
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

    public SuppliersController(ISupplierService supplierService) => _supplierService = supplierService;

    /// <summary>Tüm tedarikçileri listeler.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _supplierService.GetAllAsync(cancellationToken));

    /// <summary>Id ile tedarikçi getirir.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<SupplierResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    /// <summary>Şirkete ait tedarikçileri listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
        => Ok(await _supplierService.GetByCompanyIdAsync(companyId, cancellationToken));

    /// <summary>Yeni tedarikçi oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<SupplierResponse>> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var created = await _supplierService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Tedarikçiyi günceller.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
        => Ok(await _supplierService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Tedarikçiyi siler.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _supplierService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
