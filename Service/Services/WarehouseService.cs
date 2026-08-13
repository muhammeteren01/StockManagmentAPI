using Core.Abstractions;
using Core.Authorization;
using Core.DTOs.Warehouses;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Options;

namespace Service.Services;

/// <summary>Warehouse iş kuralları. Sysmond açıkken write → orchestrator.</summary>
public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateWarehouseRequest> _createValidator;
    private readonly IValidator<UpdateWarehouseRequest> _updateValidator;
    private readonly ISysmondWarehouseOrchestrator _sysmondWarehouse;
    private readonly SysmondOptions _sysmondOptions;

    public WarehouseService(
        IWarehouseRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateWarehouseRequest> createValidator,
        IValidator<UpdateWarehouseRequest> updateValidator,
        ISysmondWarehouseOrchestrator sysmondWarehouse,
        IOptions<SysmondOptions> sysmondOptions)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _sysmondWarehouse = sysmondWarehouse;
        _sysmondOptions = sysmondOptions.Value;
    }

    public async Task<WarehouseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : WarehouseMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(WarehouseMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<WarehouseResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureCompanyAccess(_currentUser, companyId);
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(WarehouseMapper.ToResponse).ToList();
    }

    public async Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);

        if (_sysmondOptions.Enabled)
            return await _sysmondWarehouse.CreateAsync(companyId, request, cancellationToken);

        var entity = WarehouseMapper.ToEntity(request, companyId);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WarehouseMapper.ToResponse(entity);
    }

    public async Task<WarehouseResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse bulunamadı: {id}");

        if (_sysmondOptions.Enabled && entity.ExternalSysmondId is Guid ext && ext != Guid.Empty)
            return await _sysmondWarehouse.UpdateAsync(id, request, cancellationToken);

        WarehouseMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WarehouseMapper.ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse bulunamadı: {id}");

        if (_sysmondOptions.Enabled && entity.ExternalSysmondId is Guid ext && ext != Guid.Empty)
        {
            await _sysmondWarehouse.DeleteAsync(id, cancellationToken);
            return;
        }

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
