using Core.Entities;

namespace Core.Repositories;

/// <summary>Supplier entity'sine ait veri erişim işlemleri.</summary>
public interface ISupplierRepository : IGenericRepository<Supplier>
{
    Task<IReadOnlyList<Supplier>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
