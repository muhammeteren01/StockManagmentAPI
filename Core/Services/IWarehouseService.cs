using Core.Entities;

namespace Core.Services;

/// <summary>Warehouse iş kuralları; şirket bazlı depo yönetimi.</summary>
public interface IWarehouseService : IGenericService<Warehouse>
{
    Task<IReadOnlyList<Warehouse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
