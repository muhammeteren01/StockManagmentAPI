namespace Core.Services;

/// <summary>Şifre hash ve doğrulama işlemleri.</summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
