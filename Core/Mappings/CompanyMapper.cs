using Core.DTOs.Companies;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Company entity ↔ DTO dönüşümleri.</summary>
public static class CompanyMapper
{
    public static Company ToEntity(CreateCompanyRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        TaxOffice = request.TaxOffice,
        TaxNumber = request.TaxNumber,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(Company entity, UpdateCompanyRequest request)
    {
        entity.Name = request.Name;
        entity.TaxOffice = request.TaxOffice;
        entity.TaxNumber = request.TaxNumber;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.IsActive = request.IsActive;
    }

    public static CompanyResponse ToResponse(Company entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        TaxOffice = entity.TaxOffice,
        TaxNumber = entity.TaxNumber,
        Phone = entity.Phone,
        Email = entity.Email,
        Address = entity.Address,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
