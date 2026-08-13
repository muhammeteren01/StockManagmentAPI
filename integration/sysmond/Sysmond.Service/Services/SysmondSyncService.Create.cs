using Integration.Sysmond.Core.DTOs;
using Core.DTOs.Warehouses;
using Core.Entities;
using Integration.Sysmond.Core.Mappings;
using Core.Mappings;
using Core.Validations;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

public partial class SysmondSyncService
{
    /// <inheritdoc />
    public async Task<SysmondActResponse> CreateActAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateActRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "name zorunludur.")
            ]);
        }

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var body = SysmondActMapper.ToActCreateDto(request, company.Id);
        var sysmondActId = await _actQuery.CreateActAsync(accessToken, body, cancellationToken);

        var act = SysmondActMapper.ToNewActFromCreate(request, company.Id, sysmondActId);
        await _actRepository.AddAsync(act, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sysmond act create OK: ExternalSysmondId={ExternalSysmondId}, LocalActId={LocalActId}, Name={Name}",
            sysmondActId,
            act.Id,
            act.Name);

        return SysmondActMapper.ToResponse(act);
    }

    /// <inheritdoc />
    public async Task<WarehouseResponse> CreateWarehouseAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.WarehouseCode))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Name), "name veya warehouseCode zorunludur.")
            ]);
        }

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var body = SysmondInventoryMapper.ToWarehouseCreateDto(request, company.Id);
        var sysmondWarehouseId = await _inventoryQuery.CreateWarehouseAsync(accessToken, body, cancellationToken);

        var warehouse = SysmondInventoryMapper.ToNewWarehouseFromCreate(
            request, company.Id, sysmondWarehouseId);
        await _warehouseRepository.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sysmond warehouse create OK: ExternalSysmondId={ExternalSysmondId}, LocalWarehouseId={LocalWarehouseId}, Name={Name}",
            sysmondWarehouseId,
            warehouse.Id,
            warehouse.Name);

        return WarehouseMapper.ToResponse(warehouse);
    }
}
