using Core.Abstractions;
using Core.Enums;
using Core.Exceptions;

namespace Core.Authorization;

/// <summary>Token / istekten şirket ve kullanıcı kimliği çözümleme kuralları.</summary>
public static class TenantGuard
{
    /// <summary>
    /// Yazma işlemleri için CompanyId üretir.
    /// SuperAdmin: requestCompanyId zorunlu; diğer roller: token CompanyId.
    /// </summary>
    public static Guid ResolveCompanyId(ICurrentUser currentUser, Guid? requestCompanyId)
    {
        if (currentUser.IsSuperAdmin)
        {
            if (!requestCompanyId.HasValue || requestCompanyId.Value == Guid.Empty)
                throw new InvalidOperationException("SuperAdmin için CompanyId zorunludur.");
            return requestCompanyId.Value;
        }

        if (!currentUser.CompanyId.HasValue || currentUser.CompanyId.Value == Guid.Empty)
            throw new UnauthorizedException("Şirket bilgisi bulunamadı.");

        return currentUser.CompanyId.Value;
    }

    /// <summary>Token'daki kullanıcı Id'sini zorunlu olarak döner.</summary>
    public static Guid RequireUserId(ICurrentUser currentUser)
    {
        if (!currentUser.UserId.HasValue || currentUser.UserId.Value == Guid.Empty)
            throw new UnauthorizedException("Kullanıcı kimliği bulunamadı.");
        return currentUser.UserId.Value;
    }

    /// <summary>SuperAdmin değilse yalnızca kendi şirketine erişebilir.</summary>
    public static void EnsureCompanyAccess(ICurrentUser currentUser, Guid companyId)
    {
        if (currentUser.IsSuperAdmin)
            return;

        if (!currentUser.CompanyId.HasValue || currentUser.CompanyId.Value != companyId)
            throw new UnauthorizedException("Bu şirkete erişim yetkiniz yok.");
    }

    /// <summary>Yalnızca SuperAdmin SuperAdmin rolü atayabilir.</summary>
    public static void EnsureCanAssignRole(ICurrentUser currentUser, UserRole targetRole)
    {
        if (targetRole == UserRole.SuperAdmin && !currentUser.IsSuperAdmin)
            throw new UnauthorizedException("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir.");
    }

    /// <summary>Kullanıcı oluştururken CompanyId: SuperAdmin rolünde null; aksi halde ResolveCompanyId.</summary>
    public static Guid? ResolveUserCompanyId(ICurrentUser currentUser, UserRole role, Guid? requestCompanyId)
    {
        EnsureCanAssignRole(currentUser, role);
        if (role == UserRole.SuperAdmin)
            return null;
        return ResolveCompanyId(currentUser, requestCompanyId);
    }
}
