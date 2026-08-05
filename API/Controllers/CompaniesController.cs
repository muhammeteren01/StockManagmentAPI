using Core.DTOs.Companies;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Şirket CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService) => _companyService = companyService;

    /// <summary>Tüm şirketleri listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _companyService.GetAllAsync(cancellationToken));

    /// <summary>Id ile şirket getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetByIdAsync(id, cancellationToken);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>Yeni şirket oluşturur.</summary>
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var created = await _companyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Şirketi günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
        => Ok(await _companyService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Şirketi siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _companyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
