using Core.DTOs.Warehouses;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Warehouse entity ↔ DTO dönüşümleri.</summary>
public static class WarehouseMapper
{
    public static Warehouse ToEntity(CreateWarehouseRequest request, Guid companyId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Name = request.Name,
        Location = request.Location,
        Capacity = request.Capacity,
        IsActive = true
    };

    public static void ApplyUpdate(Warehouse entity, UpdateWarehouseRequest request)
    {
        entity.Name = request.Name;
        entity.Location = request.Location;
        entity.Capacity = request.Capacity;
        entity.IsActive = request.IsActive;
    }

    public static WarehouseResponse ToResponse(Warehouse entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        Name = entity.Name,
        Location = entity.Location,
        Capacity = entity.Capacity,
        IsActive = entity.IsActive
    };
}
