using Core.Authorization;
using Core.DTOs.Acts;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Cari (Act) CRUD — Sysmond açıksa tek istekte lokal + Sysmond.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActsController : ControllerBase
{
    private readonly IActService _actService;

    public ActsController(IActService actService) => _actService = actService;

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<ActResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var act = await _actService.GetByIdAsync(id, cancellationToken);
        return act is null ? NotFound() : Ok(act);
    }

    [HttpGet("by-company/{companyId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<ActResponse>>> GetByCompany(
        Guid companyId,
        CancellationToken cancellationToken)
        => Ok(await _actService.GetByCompanyIdAsync(companyId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<ActResponse>> Create(
        [FromBody] CreateActRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _actService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<ActResponse>> Update(
        Guid id,
        [FromBody] UpdateActRequest request,
        CancellationToken cancellationToken)
        => Ok(await _actService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _actService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
