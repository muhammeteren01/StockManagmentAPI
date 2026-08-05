using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Category iş kuralları implementasyonu.</summary>
public class CategoryService : GenericService<Category>, ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<Category> validator)
        : base(repository, unitOfWork, validator)
    {
        _categoryRepository = repository;
    }

    /// <summary>Belirli şirkete ait kategorileri listeler.</summary>
    public Task<IReadOnlyList<Category>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _categoryRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    /// <summary>Yeni kategori oluşturur; Id boşsa otomatik atanır.</summary>
    public override async Task<Category> CreateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        return await base.CreateAsync(entity, cancellationToken);
    }
}
