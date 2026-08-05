using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Category iş kuralları implementasyonu.</summary>
public class CategoryService : GenericService<Category>, ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _categoryRepository = repository;
    }

    public Task<IReadOnlyList<Category>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _categoryRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    public override async Task<Category> CreateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        return await base.CreateAsync(entity, cancellationToken);
    }
}
