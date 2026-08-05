namespace Core.DTOs.Auth;

/// <summary>Login isteği.</summary>
public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
