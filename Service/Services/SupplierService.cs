using Core.DTOs.Suppliers;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Supplier iş kuralları implementasyonu (DTO).</summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSupplierRequest> _createValidator;
    private readonly IValidator<UpdateSupplierRequest> _updateValidator;

    public SupplierService(
        ISupplierRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateSupplierRequest> createValidator,
        IValidator<UpdateSupplierRequest> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Id ile tedarikçi getirir.</summary>
    public async Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : SupplierMapper.ToResponse(entity);
    }

    /// <summary>Tüm tedarikçileri listeler.</summary>
    public async Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(SupplierMapper.ToResponse).ToList();
    }

    /// <summary>Şirkete ait tedarikçileri listeler.</summary>
    public async Task<IReadOnlyList<SupplierResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(SupplierMapper.ToResponse).ToList();
    }

    /// <summary>Yeni tedarikçi oluşturur.</summary>
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var entity = SupplierMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SupplierMapper.ToResponse(entity);
    }

    /// <summary>Tedarikçiyi günceller.</summary>
    public async Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier bulunamadı: {id}");
        SupplierMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SupplierMapper.ToResponse(entity);
    }

    /// <summary>Tedarikçiyi siler.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
