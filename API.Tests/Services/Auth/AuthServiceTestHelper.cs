using Core.Abstractions;
using Core.DTOs.Auth;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Core.Validations.Auth;
using Moq;
using Service.Services.Auth;

namespace API.Tests.Services.Auth;

/// <summary>AuthService birim testleri için ortak kurulum.</summary>
internal static class AuthServiceTestHelper
{
    public static AuthService CreateSut(
        Mock<IUserRepository> userRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IPasswordService> passwordService,
        Mock<ITokenService> tokenService,
        Mock<ICurrentUser> currentUser) =>
        new(
            userRepository.Object,
            unitOfWork.Object,
            passwordService.Object,
            tokenService.Object,
            currentUser.Object,
            new RegisterRequestValidator(),
            new LoginRequestValidator());

    public static LoginRequest ValidLogin(string email = "ali@test.com", string password = "Secret1!") =>
        new() { Email = email, Password = password };

    public static RegisterRequest ValidRegister(
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

    public static User CreateUser(
        string email = "ali@test.com",
        string passwordHash = "hashed",
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
            PasswordHash = passwordHash,
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static void SetupToken(Mock<ITokenService> tokenService, string token = "jwt-token")
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        tokenService
            .Setup(t => t.CreateToken(It.IsAny<User>(), out expiresAt))
            .Returns(token);
    }

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
