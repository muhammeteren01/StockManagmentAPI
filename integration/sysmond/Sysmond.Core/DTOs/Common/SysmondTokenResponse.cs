using System.Text.Json.Serialization;

namespace Integration.Sysmond.Core.DTOs.Common;

/// <summary>Sysmondax <c>/connect/token</c> başarılı yanıtı.</summary>
public class SysmondTokenResponse
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = null!;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>Password grant + offline_access ile dönebilir; yoksa null.</summary>
    [JsonPropertyName("refresh_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefreshToken { get; set; }
}
