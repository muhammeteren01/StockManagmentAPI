using Core.Enums;

namespace Core.Abstractions;

/// <summary>İstek kapsamındaki kimlik ve şirket bağlamı (JWT claim'lerinden).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Tenant filtreleri uygulanmasın (design-time / sistem).</summary>
    bool BypassTenantFilters { get; }

    Guid? UserId { get; }
    Guid? CompanyId { get; }
    UserRole? Role { get; }
    bool IsSuperAdmin { get; }

    /// <summary>Authenticated şirket kullanıcısı için filtre aktif.</summary>
    bool ApplyTenantFilter { get; }
}
