using System.Security.Claims;
using Core.Abstractions;
using Core.Enums;

namespace API.Services;

/// <summary>HTTP context claim'lerinden ICurrentUser üretir.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool BypassTenantFilters => false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var value = Principal?.FindFirstValue("company_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;

    public bool ApplyTenantFilter => IsAuthenticated && !IsSuperAdmin && !BypassTenantFilters;
}
