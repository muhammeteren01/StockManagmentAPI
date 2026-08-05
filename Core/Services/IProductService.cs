using Core.Entities;

namespace Core.Services;

/// <summary>Product iş kuralları; SKU uniqueness ve ürün kartı yönetimi.</summary>
public interface IProductService : IGenericService<Product>
{
    Task<IReadOnlyList<Product>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
