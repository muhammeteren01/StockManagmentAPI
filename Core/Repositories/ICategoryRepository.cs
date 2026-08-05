using Core.Entities;

namespace Core.Repositories;

/// <summary>Category entity'sine ait veri erişim işlemleri.</summary>
public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IReadOnlyList<Category>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
