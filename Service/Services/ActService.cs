using Core.Abstractions;
using Core.Authorization;
using Core.DTOs.Acts;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Options;

namespace Service.Services;

/// <summary>Act (cari) iş kuralları. Sysmond açıkken write → orchestrator.</summary>
public class ActService : IActService
{
    private readonly IActRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ISysmondActOrchestrator _sysmondAct;
    private readonly SysmondOptions _sysmondOptions;

    public ActService(
        IActRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ISysmondActOrchestrator sysmondAct,
        IOptions<SysmondOptions> sysmondOptions)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _sysmondAct = sysmondAct;
        _sysmondOptions = sysmondOptions.Value;
    }

    public async Task<ActResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ActMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<ActResponse>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureCompanyAccess(_currentUser, companyId);
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(ActMapper.ToResponse).ToList();
    }

    public async Task<ActResponse> CreateAsync(CreateActRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("name zorunludur.");

        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);

        if (_sysmondOptions.Enabled)
            return await _sysmondAct.CreateAsync(companyId, request, cancellationToken);

        var entity = ActMapper.ToEntity(request, companyId);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ActMapper.ToResponse(entity);
    }

    public async Task<ActResponse> UpdateAsync(
        Guid id,
        UpdateActRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Act bulunamadı: {id}");

        TenantGuard.EnsureCompanyAccess(_currentUser, entity.CompanyId);

        if (_sysmondOptions.Enabled && entity.ExternalSysmondId != Guid.Empty)
            return await _sysmondAct.UpdateAsync(id, request, cancellationToken);

        ActMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ActMapper.ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Act bulunamadı: {id}");

        TenantGuard.EnsureCompanyAccess(_currentUser, entity.CompanyId);

        if (_sysmondOptions.Enabled && entity.ExternalSysmondId != Guid.Empty)
        {
            await _sysmondAct.DeleteAsync(id, cancellationToken);
            return;
        }

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
