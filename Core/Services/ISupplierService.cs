using Core.DTOs.Suppliers;

namespace Core.Services;

/// <summary>Supplier iş kuralları (DTO tabanlı).</summary>
public interface ISupplierService
{
    Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
