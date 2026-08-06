using Core.DTOs.Companies;

namespace API.Tests.Controllers.Companies;

/// <summary>Companies controller testleri için ortak yardımcılar.</summary>
internal static class CompaniesTestHelper
{
    public static CompanyResponse CreateCompanyResponse(
        Guid? id = null,
        string name = "Acme A.Ş.",
        bool isActive = true) => new()
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

    public static CreateCompanyRequest CreateCreateRequest(string name = "Acme A.Ş.") => new()
    {
        Name = name,
        TaxOffice = "Kadıköy",
        TaxNumber = "1234567890",
        Phone = "+905551112233",
        Email = "info@acme.test",
        Address = "İstanbul"
    };

    public static UpdateCompanyRequest CreateUpdateRequest(string name = "Acme Güncel", bool isActive = true) => new()
    {
        Name = name,
        TaxOffice = "Üsküdar",
        TaxNumber = "0987654321",
        Phone = "+905559998877",
        Email = "new@acme.test",
        Address = "Ankara",
        IsActive = isActive
    };
}
