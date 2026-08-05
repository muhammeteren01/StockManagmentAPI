using Core.Abstractions;
using Core.Enums;

namespace Repository.Data;

/// <summary>EF design-time / migration için tenant filtresi bypass.</summary>
public sealed class DesignTimeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;
    public bool BypassTenantFilters => true;
    public Guid? UserId => null;
    public Guid? CompanyId => null;
    public UserRole? Role => null;
    public bool IsSuperAdmin => false;
    public bool ApplyTenantFilter => false;
}
