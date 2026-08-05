using Core.Enums;

namespace Core.DTOs.Auth;

/// <summary>Login/Register yanıtı; JWT ve kullanıcı özeti.</summary>
public class AuthResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public UserRole Role { get; set; }
}
