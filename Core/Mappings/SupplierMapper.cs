using Core.DTOs.Suppliers;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Supplier entity ↔ DTO dönüşümleri.</summary>
public static class SupplierMapper
{
    public static Supplier ToEntity(CreateSupplierRequest request, Guid companyId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        CompanyName = request.CompanyName,
        ContactName = request.ContactName,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address,
        TaxNumber = request.TaxNumber,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(Supplier entity, UpdateSupplierRequest request)
    {
        entity.CompanyName = request.CompanyName;
        entity.ContactName = request.ContactName;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.TaxNumber = request.TaxNumber;
    }

    public static SupplierResponse ToResponse(Supplier entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        CompanyName = entity.CompanyName,
        ContactName = entity.ContactName,
        Phone = entity.Phone,
        Email = entity.Email,
        Address = entity.Address,
        TaxNumber = entity.TaxNumber,
        CreatedAt = entity.CreatedAt
    };
}
