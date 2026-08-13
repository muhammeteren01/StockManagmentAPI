using System.Security.Claims;
using Core.Authorization;
using Core.DTOs.Auth;
using Core.Services;
using Integration.Sysmond.Core.DTOs;
using Integration.Sysmond.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Kimlik doğrulama endpoint'leri (register, login, me, sysmond-token).</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISysmondTokenService _sysmondTokenService;

    public AuthController(IAuthService authService, ISysmondTokenService sysmondTokenService)
    {
        _authService = authService;
        _sysmondTokenService = sysmondTokenService;
    }

    /// <summary>Yeni kullanıcı kaydı; JWT döner. Yalnızca SuperAdmin / CompanyAdmin.</summary>
    [HttpPost("register")]
    [Authorize(Roles = AppRoles.CompanyAdmins)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Giriş; JWT döner.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Sysmondax OAuth access token alır (yapılandırmadaki username/password + client credentials ile password grant).
    /// </summary>
    [HttpPost("sysmond-token")]
    [AllowAnonymous]
    public async Task<ActionResult<SysmondTokenResponse>> GetSysmondToken(CancellationToken cancellationToken)
    {
        var response = await _sysmondTokenService.GetAccessTokenAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>Token'daki geçerli kullanıcı bilgilerini döner.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            name = User.FindFirst(ClaimTypes.Name)?.Value,
            role = User.FindFirst(ClaimTypes.Role)?.Value,
            companyId = User.FindFirst("company_id")?.Value
        });
    }
}
