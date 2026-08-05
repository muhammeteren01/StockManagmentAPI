using Core.DTOs.Warehouses;

namespace Core.Services;

/// <summary>Warehouse iş kuralları (DTO tabanlı).</summary>
public interface IWarehouseService
{
    Task<WarehouseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
