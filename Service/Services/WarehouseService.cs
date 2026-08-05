using Core.DTOs.Warehouses;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Warehouse iş kuralları implementasyonu (DTO).</summary>
public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateWarehouseRequest> _createValidator;
    private readonly IValidator<UpdateWarehouseRequest> _updateValidator;

    public WarehouseService(
        IWarehouseRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateWarehouseRequest> createValidator,
        IValidator<UpdateWarehouseRequest> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Id ile depo getirir.</summary>
    public async Task<WarehouseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : WarehouseMapper.ToResponse(entity);
    }

    /// <summary>Tüm depoları listeler.</summary>
    public async Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(WarehouseMapper.ToResponse).ToList();
    }

    /// <summary>Şirkete ait depoları listeler.</summary>
    public async Task<IReadOnlyList<WarehouseResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(WarehouseMapper.ToResponse).ToList();
    }

    /// <summary>Yeni depo oluşturur.</summary>
    public async Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var entity = WarehouseMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WarehouseMapper.ToResponse(entity);
    }

    /// <summary>Depoyu günceller.</summary>
    public async Task<WarehouseResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse bulunamadı: {id}");
        WarehouseMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WarehouseMapper.ToResponse(entity);
    }

    /// <summary>Depoyu siler.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
