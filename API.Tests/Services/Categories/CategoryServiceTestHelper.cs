using Core.Abstractions;
using Core.DTOs.Categories;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.Categories;
using Moq;
using Service.Services;

namespace API.Tests.Services.Categories;

/// <summary>CategoryService birim testleri için ortak kurulum.</summary>
internal static class CategoryServiceTestHelper
{
    public static CategoryService CreateSut(
        Mock<ICategoryRepository> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreateCategoryRequestValidator(),
            new UpdateCategoryRequestValidator());

    public static CreateCategoryRequest ValidCreate(
        string name = "Elektronik",
        string? description = "Elektronik ürünler",
        Guid? companyId = null) =>
        new()
        {
            CompanyId = companyId ?? Guid.NewGuid(),
            Name = name,
            Description = description
        };

    public static UpdateCategoryRequest ValidUpdate(
        string name = "Güncel Kategori",
        string? description = "Güncel açıklama") =>
        new()
        {
            Name = name,
            Description = description
        };

    public static Category CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        string name = "Elektronik",
        string? description = "Elektronik ürünler") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            Name = name,
            Description = description
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
