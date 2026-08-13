using Core.DTOs.Warehouses;
using Core.Repositories;
using Core.Validations;
using FluentValidation.Results;
using Integration.Sysmond.Core.Mappings;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Services;

namespace Integration.Sysmond.Service.Orchestration;

/// <summary>Warehouse write → Sysmond warehouse + lokal Warehouse.</summary>
public sealed class SysmondWarehouseOrchestrator : ISysmondWarehouseOrchestrator
{
    private readonly ISysmondAccessTokenProvider _tokenProvider;
    private readonly ISysmondSyncService _syncService;
    private readonly IWarehouseRepository _warehouseRepository;

    public SysmondWarehouseOrchestrator(
        ISysmondAccessTokenProvider tokenProvider,
        ISysmondSyncService syncService,
        IWarehouseRepository warehouseRepository)
    {
        _tokenProvider = tokenProvider;
        _syncService = syncService;
        _warehouseRepository = warehouseRepository;
    }

    /// <inheritdoc />
    public async Task<WarehouseResponse> CreateAsync(
        Guid companyId,
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (companyId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("companyId", "companyId zorunludur.")]);

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var body = DomainToSysmondMapper.ToCreateWarehouseRequest(request);
        return await _syncService.CreateWarehouseAsync(companyId, token, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WarehouseResponse> UpdateAsync(
        Guid warehouseId,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse bulunamadı: {warehouseId}");

        if (warehouse.ExternalSysmondId is null || warehouse.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException(
                "Depo Sysmond ExternalSysmondId'si yok; önce sync veya Sysmond üzerinden create gerekir.");

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var body = DomainToSysmondMapper.ToUpdateWarehouseRequest(request);
        return await _syncService.UpdateWarehouseAsync(
            warehouse.CompanyId, token, warehouse.ExternalSysmondId.Value, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse bulunamadı: {warehouseId}");

        if (warehouse.ExternalSysmondId is null || warehouse.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException(
                "Depo Sysmond ExternalSysmondId'si yok; yalnızca lokal silme için Sysmond:Enabled=false kullanın.");

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _syncService.DeleteWarehouseAsync(
            warehouse.CompanyId, token, warehouse.ExternalSysmondId.Value, cancellationToken);
    }
}
