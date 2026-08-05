using Core.Enums;

namespace Core.DTOs.Auth;

/// <summary>Kayıt (register) isteği.</summary>
public class RegisterRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public Guid? CompanyId { get; set; }
    public UserRole Role { get; set; } = UserRole.Staff;
}
