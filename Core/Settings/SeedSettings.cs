namespace Core.Settings;

/// <summary>İlk SuperAdmin seed ayarları (appsettings SeedSettings bölümü).</summary>
public class SeedSettings
{
    public const string SectionName = "SeedSettings";

    /// <summary>true ise uygulama açılışında eksik SuperAdmin eklenir.</summary>
    public bool Enabled { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Super";
    public string LastName { get; set; } = "Admin";
}
