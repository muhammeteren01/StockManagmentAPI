using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Warehouse iş kuralları implementasyonu.</summary>
public class WarehouseService : GenericService<Warehouse>, IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseService(IWarehouseRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _warehouseRepository = repository;
    }

    public Task<IReadOnlyList<Warehouse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _warehouseRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    public override async Task<Warehouse> CreateAsync(Warehouse entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        return await base.CreateAsync(entity, cancellationToken);
    }
}
