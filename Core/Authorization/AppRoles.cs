using Core.Enums;

namespace Core.Authorization;

/// <summary>
/// JWT Role claim ve [Authorize(Roles)] için sabit rol adları.
/// <see cref="UserRole"/> enum adlarıyla birebir eşleşir.
/// </summary>
public static class AppRoles
{
    public const string SuperAdmin = nameof(UserRole.SuperAdmin);
    public const string CompanyAdmin = nameof(UserRole.CompanyAdmin);
    public const string Manager = nameof(UserRole.Manager);
    public const string Staff = nameof(UserRole.Staff);

    /// <summary>Kimliği doğrulanmış tüm roller.</summary>
    public const string All = $"{SuperAdmin},{CompanyAdmin},{Manager},{Staff}";

    /// <summary>Şirket / kullanıcı yönetimi (SuperAdmin + CompanyAdmin).</summary>
    public const string CompanyAdmins = $"{SuperAdmin},{CompanyAdmin}";

    /// <summary>Master data ve operasyon yazma (Staff hariç).</summary>
    public const string Writers = $"{SuperAdmin},{CompanyAdmin},{Manager}";

    /// <summary>Stok hareketi oluşturma (tüm roller).</summary>
    public const string StockOps = All;

    /// <summary>Yalnızca SuperAdmin.</summary>
    public const string SuperAdminOnly = SuperAdmin;
}
