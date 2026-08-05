using Core.Entities;

namespace Core.Repositories;

/// <summary>Warehouse entity'sine ait veri erişim işlemleri.</summary>
public interface IWarehouseRepository : IGenericRepository<Warehouse>
{
    Task<IReadOnlyList<Warehouse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
