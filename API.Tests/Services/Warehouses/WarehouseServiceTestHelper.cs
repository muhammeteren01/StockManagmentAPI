using Core.Abstractions;
using Core.DTOs.Warehouses;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.Warehouses;
using Moq;
using Service.Services;

namespace API.Tests.Services.Warehouses;

/// <summary>WarehouseService birim testleri için ortak kurulum.</summary>
internal static class WarehouseServiceTestHelper
{
    public static WarehouseService CreateSut(
        Mock<IWarehouseRepository> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreateWarehouseRequestValidator(),
            new UpdateWarehouseRequestValidator());

    public static CreateWarehouseRequest ValidCreate(
        string name = "Merkez Depo",
        string location = "İstanbul",
        int? capacity = 1000,
        Guid? companyId = null) =>
        new()
        {
            CompanyId = companyId ?? Guid.NewGuid(),
            Name = name,
            Location = location,
            Capacity = capacity
        };

    public static UpdateWarehouseRequest ValidUpdate(
        string name = "Güncel Depo",
        string location = "Ankara",
        int? capacity = 2000,
        bool isActive = true) =>
        new()
        {
            Name = name,
            Location = location,
            Capacity = capacity,
            IsActive = isActive
        };

    public static Warehouse CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        string name = "Merkez Depo",
        string location = "İstanbul",
        int? capacity = 1000,
        bool isActive = true) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            Name = name,
            Location = location,
            Capacity = capacity,
            IsActive = isActive
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
