using Core.Entities;

namespace Core.Repositories;

/// <summary>Company entity'sine ait veri erişim işlemleri.</summary>
public interface ICompanyRepository : IGenericRepository<Company>
{
    Task<Company?> GetByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default);
}
