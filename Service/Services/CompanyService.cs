using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Company iş kuralları implementasyonu.</summary>
public class CompanyService : GenericService<Company>, ICompanyService
{
    public CompanyService(
        ICompanyRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<Company> validator)
        : base(repository, unitOfWork, validator)
    {
    }

    /// <summary>Yeni şirket oluşturur; Id ve CreatedAt boşsa otomatik atanır.</summary>
    public override async Task<Company> CreateAsync(Company entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity, cancellationToken);
    }
}
