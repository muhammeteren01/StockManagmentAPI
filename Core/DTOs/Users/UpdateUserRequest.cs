using Core.Enums;

namespace Core.DTOs.Users;

/// <summary>Kullanıcı güncelleme isteği.</summary>
public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
