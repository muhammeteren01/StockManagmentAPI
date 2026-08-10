using Core.Entities;

namespace Core.Repositories;

/// <summary>Product entity'sine ait veri erişim işlemleri.</summary>
public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetBySkuAsync(Guid companyId, string sku, CancellationToken cancellationToken = default);
    Task<Product?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
