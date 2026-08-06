using Core.Abstractions;
using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Core.Validations.Users;
using Moq;
using Service.Services;

namespace API.Tests.Services.Users;

/// <summary>UserService birim testleri için ortak kurulum.</summary>
internal static class UserServiceTestHelper
{
    public static UserService CreateSut(
        Mock<IUserRepository> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IPasswordService> passwordService,
        Mock<ICurrentUser> currentUser) =>
        new(
            repository.Object,
            unitOfWork.Object,
            passwordService.Object,
            currentUser.Object,
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator());

    public static CreateUserRequest ValidCreate(
        string email = "ali@test.com",
        UserRole role = UserRole.Staff,
        Guid? companyId = null) =>
        new()
        {
            FirstName = "Ali",
            LastName = "Veli",
            Email = email,
            Password = "Secret1!",
            CompanyId = companyId ?? Guid.NewGuid(),
            Role = role
        };

    public static UpdateUserRequest ValidUpdate(
        string email = "ali.updated@test.com",
        UserRole role = UserRole.Manager,
        Guid? companyId = null,
        bool isActive = true) =>
        new()
        {
            FirstName = "Ali",
            LastName = "Yılmaz",
            Email = email,
            CompanyId = companyId ?? Guid.NewGuid(),
            Role = role,
            IsActive = isActive
        };

    public static User CreateEntity(
        string email = "ali@test.com",
        UserRole role = UserRole.Staff,
        Guid? companyId = null,
        bool isActive = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Veli",
            Email = email,
            PasswordHash = "hashed",
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static void SetupCompanyAdminCurrentUser(Mock<ICurrentUser> currentUser, Guid companyId)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        currentUser.SetupGet(c => c.CompanyId).Returns(companyId);
        currentUser.SetupGet(c => c.Role).Returns(UserRole.CompanyAdmin);
    }

    public static void SetupSuperAdminCurrentUser(Mock<ICurrentUser> currentUser)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(true);
        currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        currentUser.SetupGet(c => c.Role).Returns(UserRole.SuperAdmin);
    }
}
