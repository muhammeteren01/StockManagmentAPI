namespace Integration.Sysmond.Core.Settings;

/// <summary>
/// Sysmondax OAuth ayarları (appsettings <c>Sysmond</c> bölümü).
/// Username / Password / ClientId / ClientSecret değerlerini User Secrets veya ortam değişkeni ile verin; repoya yazmayın.
/// </summary>
public class SysmondOptions
{
    public const string SectionName = "Sysmond";
    public const string HttpClientName = "Sysmond";

    public const string DefaultScope = "address email phone profile roles offline_access Sysmond";

    /// <summary>
    /// false ise domain write path'leri yalnızca lokal CRUD yapar; Sysmond HTTP çağrısı yapılmaz.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Örn. https://api.sysmondax.com</summary>
    public string BaseUrl { get; set; } = "https://api.sysmondax.com";

    /// <summary>Sysmond kullanıcı adı (User Secrets: Sysmond:Username).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Sysmond şifresi (User Secrets: Sysmond:Password).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>OAuth client_id (User Secrets: Sysmond:ClientId).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client_secret (User Secrets: Sysmond:ClientSecret).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>OAuth scope; varsayılan password-grant browser formu ile aynı.</summary>
    public string Scope { get; set; } = DefaultScope;
}
