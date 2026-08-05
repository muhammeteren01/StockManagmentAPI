using Core.Enums;

namespace Core.DTOs.Users;

/// <summary>Kullanıcı yanıt modeli (şifre hash dönmez).</summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
