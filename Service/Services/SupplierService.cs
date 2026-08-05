using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Supplier iş kuralları implementasyonu.</summary>
public class SupplierService : GenericService<Supplier>, ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _supplierRepository = repository;
    }

    public Task<IReadOnlyList<Supplier>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _supplierRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    public override async Task<Supplier> CreateAsync(Supplier entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity, cancellationToken);
    }
}
