using Core.Abstractions;
using Core.Authorization;
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
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateSupplierRequest> _createValidator;
    private readonly IValidator<UpdateSupplierRequest> _updateValidator;

    public SupplierService(
        ISupplierRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateSupplierRequest> createValidator,
        IValidator<UpdateSupplierRequest> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : SupplierMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(SupplierMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<SupplierResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureCompanyAccess(_currentUser, companyId);
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(SupplierMapper.ToResponse).ToList();
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);
        var entity = SupplierMapper.ToEntity(request, companyId);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SupplierMapper.ToResponse(entity);
    }

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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
