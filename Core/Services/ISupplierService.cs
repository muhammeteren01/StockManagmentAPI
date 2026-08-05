using Core.Entities;

namespace Core.Services;

/// <summary>Supplier iş kuralları; şirket bazlı tedarikçi yönetimi.</summary>
public interface ISupplierService : IGenericService<Supplier>
{
    Task<IReadOnlyList<Supplier>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
