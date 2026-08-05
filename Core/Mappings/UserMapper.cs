using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>User entity ↔ DTO dönüşümleri.</summary>
public static class UserMapper
{
    public static User ToEntity(CreateUserRequest request, string passwordHash, Guid? companyId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        PasswordHash = passwordHash,
        Role = request.Role,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(User entity, UpdateUserRequest request, Guid? companyId)
    {
        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.Email = request.Email;
        entity.CompanyId = companyId;
        entity.Role = request.Role;
        entity.IsActive = request.IsActive;
    }

    public static UserResponse ToResponse(User entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Email = entity.Email,
        Role = entity.Role,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
