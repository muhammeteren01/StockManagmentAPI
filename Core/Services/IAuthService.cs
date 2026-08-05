using Core.DTOs.Auth;

namespace Core.Services;

/// <summary>Kayıt ve giriş işlemleri.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
