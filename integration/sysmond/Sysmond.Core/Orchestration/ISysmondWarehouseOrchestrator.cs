using Core.DTOs.Warehouses;

namespace Integration.Sysmond.Core.Orchestration;

/// <summary>Tek istek: lokal Warehouse + Sysmond warehouse yazma.</summary>
public interface ISysmondWarehouseOrchestrator
{
    Task<WarehouseResponse> CreateAsync(
        Guid companyId,
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task<WarehouseResponse> UpdateAsync(
        Guid warehouseId,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
