using Core.DTOs.Categories;

namespace Core.Services;

/// <summary>Category iş kuralları (DTO tabanlı).</summary>
public interface ICategoryService
{
    Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
