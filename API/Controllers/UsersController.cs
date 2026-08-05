using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Kullanıcı CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Tüm kullanıcıları listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<User>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile kullanıcı getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Şirkete ait kullanıcıları listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<IReadOnlyList<User>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    /// <summary>E-posta ile kullanıcı getirir.</summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<User>> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByEmailAsync(email, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Yeni kullanıcı oluşturur (şifre hash olarak beklenir; kayıt için /api/auth/register tercih edilir).</summary>
    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] User user, CancellationToken cancellationToken)
    {
        var created = await _userService.CreateAsync(user, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Kullanıcıyı günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] User user, CancellationToken cancellationToken)
    {
        if (id != user.Id)
            return BadRequest("Id uyuşmuyor.");

        await _userService.UpdateAsync(user, cancellationToken);
        return NoContent();
    }

    /// <summary>Kullanıcıyı siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
