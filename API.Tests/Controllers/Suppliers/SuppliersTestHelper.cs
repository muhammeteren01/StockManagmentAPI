using Core.DTOs.Suppliers;

namespace API.Tests.Controllers.Suppliers;

/// <summary>Suppliers controller testleri için ortak yardımcılar.</summary>
internal static class SuppliersTestHelper
{
    public static SupplierResponse CreateSupplierResponse(
        Guid? id = null,
        Guid? companyId = null,
        string companyName = "Acme Tedarik",
        string contactName = "Ali Veli",
        string phone = "+905551112233",
        string email = "ali@acme.com",
        string address = "İstanbul",
        string taxNumber = "1234567890") => new()
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

    public static CreateSupplierRequest CreateCreateRequest(
        string companyName = "Acme Tedarik",
        string contactName = "Ali Veli",
        string phone = "+905551112233",
        string email = "ali@acme.com",
        string address = "İstanbul",
        string taxNumber = "1234567890",
        Guid? companyId = null) => new()
    {
        CompanyId = companyId ?? Guid.NewGuid(),
        CompanyName = companyName,
        ContactName = contactName,
        Phone = phone,
        Email = email,
        Address = address,
        TaxNumber = taxNumber
    };

    public static UpdateSupplierRequest CreateUpdateRequest(
        string companyName = "Güncel Tedarik",
        string contactName = "Ayşe Yılmaz",
        string phone = "+905559998877",
        string email = "ayse@guncel.com",
        string address = "Ankara",
        string taxNumber = "0987654321") => new()
    {
        CompanyName = companyName,
        ContactName = contactName,
        Phone = phone,
        Email = email,
        Address = address,
        TaxNumber = taxNumber
    };
}
