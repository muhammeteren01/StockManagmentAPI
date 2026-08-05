using Core.Enums;

namespace Core.Entities;

/// <summary>
/// Sistem kullanıcısı. CompanyId null ise sistem geneli yöneticisidir (SuperAdmin);
/// dolu ise ilgili şirketin kullanıcısıdır. Yetki seviyesini Role belirler.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Null ise Sistem Admini (Super Admin)</summary>
    public Guid? CompanyId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Kullanıcı yetki seviyesi. DB'ye string olarak yazılır (DbContext'te HasConversion ile).</summary>
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<StockTransfer> StockTransfers { get; set; } = new List<StockTransfer>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
