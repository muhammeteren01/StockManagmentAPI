using Core.DTOs.Acts;
using Core.Repositories;
using Core.Validations;
using FluentValidation.Results;
using Integration.Sysmond.Core.Mappings;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Services;

namespace Integration.Sysmond.Service.Orchestration;

/// <summary>Act write → Sysmond act + lokal Act.</summary>
public sealed class SysmondActOrchestrator : ISysmondActOrchestrator
{
    private readonly ISysmondAccessTokenProvider _tokenProvider;
    private readonly ISysmondSyncService _syncService;
    private readonly IActRepository _actRepository;

    public SysmondActOrchestrator(
        ISysmondAccessTokenProvider tokenProvider,
        ISysmondSyncService syncService,
        IActRepository actRepository)
    {
        _tokenProvider = tokenProvider;
        _syncService = syncService;
        _actRepository = actRepository;
    }

    /// <inheritdoc />
    public async Task<ActResponse> CreateAsync(
        Guid companyId,
        CreateActRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (companyId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("companyId", "companyId zorunludur.")]);

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var body = DomainToSysmondMapper.ToCreateActRequest(request);
        var remote = await _syncService.CreateActAsync(companyId, token, body, cancellationToken);
        return DomainToSysmondMapper.ToActResponse(remote, companyId);
    }

    /// <inheritdoc />
    public async Task<ActResponse> UpdateAsync(
        Guid actId,
        UpdateActRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var act = await _actRepository.GetByIdAsync(actId, cancellationToken)
            ?? throw new KeyNotFoundException($"Act bulunamadı: {actId}");

        if (act.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException("Act ExternalSysmondId boş; önce sync gerekir.");

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var body = DomainToSysmondMapper.ToUpdateActRequest(request);
        var remote = await _syncService.UpdateActAsync(
            act.CompanyId, token, act.ExternalSysmondId, body, cancellationToken);
        return DomainToSysmondMapper.ToActResponse(remote, act.CompanyId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid actId, CancellationToken cancellationToken = default)
    {
        var act = await _actRepository.GetByIdAsync(actId, cancellationToken)
            ?? throw new KeyNotFoundException($"Act bulunamadı: {actId}");

        if (act.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException("Act ExternalSysmondId boş; yalnızca lokal silme için Sysmond:Enabled=false.");

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _syncService.DeleteActAsync(act.CompanyId, token, act.ExternalSysmondId, cancellationToken);
    }
}
