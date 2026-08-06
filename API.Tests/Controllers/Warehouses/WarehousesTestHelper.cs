using Core.DTOs.Warehouses;

namespace API.Tests.Controllers.Warehouses;

/// <summary>Warehouses controller testleri için ortak yardımcılar.</summary>
internal static class WarehousesTestHelper
{
    public static WarehouseResponse CreateWarehouseResponse(
        Guid? id = null,
        Guid? companyId = null,
        string name = "Merkez Depo",
        string location = "İstanbul",
        int? capacity = 1000,
        bool isActive = true) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        Name = name,
        Location = location,
        Capacity = capacity,
        IsActive = isActive
    };

    public static CreateWarehouseRequest CreateCreateRequest(
        string name = "Merkez Depo",
        string location = "İstanbul",
        int? capacity = 1000,
        Guid? companyId = null) => new()
    {
        CompanyId = companyId ?? Guid.NewGuid(),
        Name = name,
        Location = location,
        Capacity = capacity
    };

    public static UpdateWarehouseRequest CreateUpdateRequest(
        string name = "Güncel Depo",
        string location = "Ankara",
        int? capacity = 2000,
        bool isActive = true) => new()
    {
        Name = name,
        Location = location,
        Capacity = capacity,
        IsActive = isActive
    };
}
