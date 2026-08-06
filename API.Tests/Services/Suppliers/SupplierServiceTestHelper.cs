using Core.Abstractions;
using Core.DTOs.Suppliers;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.Suppliers;
using Moq;
using Service.Services;

namespace API.Tests.Services.Suppliers;

/// <summary>SupplierService birim testleri için ortak kurulum.</summary>
internal static class SupplierServiceTestHelper
{
    public static SupplierService CreateSut(
        Mock<ISupplierRepository> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreateSupplierRequestValidator(),
            new UpdateSupplierRequestValidator());

    public static CreateSupplierRequest ValidCreate(
        string companyName = "Acme Tedarik",
        string contactName = "Ali Veli",
        string phone = "+905551112233",
        string email = "ali@acme.com",
        string address = "İstanbul",
        string taxNumber = "1234567890",
        Guid? companyId = null) =>
        new()
        {
            CompanyId = companyId ?? Guid.NewGuid(),
            CompanyName = companyName,
            ContactName = contactName,
            Phone = phone,
            Email = email,
            Address = address,
            TaxNumber = taxNumber
        };

    public static UpdateSupplierRequest ValidUpdate(
        string companyName = "Güncel Tedarik",
        string contactName = "Ayşe Yılmaz",
        string phone = "+905559998877",
        string email = "ayse@guncel.com",
        string address = "Ankara",
        string taxNumber = "0987654321") =>
        new()
        {
            CompanyName = companyName,
            ContactName = contactName,
            Phone = phone,
            Email = email,
            Address = address,
            TaxNumber = taxNumber
        };

    public static Supplier CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        string companyName = "Acme Tedarik",
        string contactName = "Ali Veli",
        string phone = "+905551112233",
        string email = "ali@acme.com",
        string address = "İstanbul",
        string taxNumber = "1234567890") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            CompanyName = companyName,
            ContactName = contactName,
            Phone = phone,
            Email = email,
            Address = address,
            TaxNumber = taxNumber,
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
