using Core.DTOs.Users;
using Core.Enums;

namespace API.Tests.Controllers.Users;

/// <summary>Users controller testleri için ortak yardımcılar.</summary>
internal static class UsersTestHelper
{
    public static UserResponse CreateUserResponse(
        Guid? id = null,
        Guid? companyId = null,
        string email = "ali@test.com",
        UserRole role = UserRole.Staff,
        bool isActive = true) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        FirstName = "Ali",
        LastName = "Veli",
        Email = email,
        Role = role,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    public static CreateUserRequest CreateCreateRequest(
        string email = "ali@test.com",
        UserRole role = UserRole.Staff,
        Guid? companyId = null) => new()
    {
        FirstName = "Ali",
        LastName = "Veli",
        Email = email,
        Password = "Secret1!",
        CompanyId = companyId ?? Guid.NewGuid(),
        Role = role
    };

    public static UpdateUserRequest CreateUpdateRequest(
        string email = "ali.updated@test.com",
        UserRole role = UserRole.Manager,
        Guid? companyId = null,
        bool isActive = true) => new()
    {
        FirstName = "Ali",
        LastName = "Yılmaz",
        Email = email,
        CompanyId = companyId ?? Guid.NewGuid(),
        Role = role,
        IsActive = isActive
    };
}
