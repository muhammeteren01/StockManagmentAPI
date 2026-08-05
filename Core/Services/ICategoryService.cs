using Core.Entities;

namespace Core.Services;

/// <summary>Category iş kuralları; şirket bazlı ürün kategorisi yönetimi.</summary>
public interface ICategoryService : IGenericService<Category>
{
    Task<IReadOnlyList<Category>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
