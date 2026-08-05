using Core.Services;

namespace Service.Services.Auth;

/// <summary>BCrypt ile şifre hash ve doğrulama.</summary>
public class PasswordService : IPasswordService
{
    /// <summary>Şifreyi hash'ler.</summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>Şifreyi hash ile karşılaştırır.</summary>
    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
