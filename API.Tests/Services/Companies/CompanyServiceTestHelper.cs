using Core.DTOs.Companies;
using Core.Entities;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.Companies;
using Moq;
using Service.Services;

namespace API.Tests.Services.Companies;

/// <summary>CompanyService birim testleri için ortak kurulum.</summary>
internal static class CompanyServiceTestHelper
{
    public static CompanyService CreateSut(
        Mock<ICompanyRepository> repository,
        Mock<IUnitOfWork> unitOfWork) =>
        new(
            repository.Object,
            unitOfWork.Object,
            new CreateCompanyRequestValidator(),
            new UpdateCompanyRequestValidator());

    public static CreateCompanyRequest ValidCreate(string name = "Acme A.Ş.") => new()
    {
        Name = name,
        TaxOffice = "Kadıköy",
        TaxNumber = "1234567890",
        Phone = "+905551112233",
        Email = "info@acme.test",
        Address = "İstanbul"
    };

    public static UpdateCompanyRequest ValidUpdate(string name = "Acme Güncel", bool isActive = true) => new()
    {
        Name = name,
        TaxOffice = "Üsküdar",
        TaxNumber = "0987654321",
        Phone = "+905559998877",
        Email = "new@acme.test",
        Address = "Ankara",
        IsActive = isActive
    };

    public static Company CreateEntity(
        Guid? id = null,
        string name = "Acme A.Ş.",
        bool isActive = true) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            TaxOffice = "Kadıköy",
            TaxNumber = "1234567890",
            Phone = "+905551112233",
            Email = "info@acme.test",
            Address = "İstanbul",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
}
