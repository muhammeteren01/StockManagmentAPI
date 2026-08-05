using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Warehouse iş kuralları implementasyonu.</summary>
public class WarehouseService : GenericService<Warehouse>, IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseService(
        IWarehouseRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<Warehouse> validator)
        : base(repository, unitOfWork, validator)
    {
        _warehouseRepository = repository;
    }

    /// <summary>Belirli şirkete ait depoları listeler.</summary>
    public Task<IReadOnlyList<Warehouse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _warehouseRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    /// <summary>Yeni depo oluşturur; Id boşsa otomatik atanır.</summary>
    public override async Task<Warehouse> CreateAsync(Warehouse entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        return await base.CreateAsync(entity, cancellationToken);
    }
}
