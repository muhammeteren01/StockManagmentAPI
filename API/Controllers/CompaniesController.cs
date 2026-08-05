using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Şirket (tenant) CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>Tüm şirketleri listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Company>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _companyService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile şirket getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Company>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetByIdAsync(id, cancellationToken);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>Yeni şirket oluşturur.</summary>
    [HttpPost]
    public async Task<ActionResult<Company>> Create([FromBody] Company company, CancellationToken cancellationToken)
    {
        var created = await _companyService.CreateAsync(company, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Şirketi günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Company company, CancellationToken cancellationToken)
    {
        if (id != company.Id)
            return BadRequest("Id uyuşmuyor.");

        await _companyService.UpdateAsync(company, cancellationToken);
        return NoContent();
    }

    /// <summary>Şirketi siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _companyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
