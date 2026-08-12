using Core.Entities;
using Core.Validations;
using FluentValidation.Results;

namespace Service.Services.Sysmond;

public partial class SysmondSyncService
{
    /// <inheritdoc />
    public async Task DeleteActAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondActId,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        if (sysmondActId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("id", "Sysmond act id zorunludur.")]);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var localAct = await _actRepository.GetByExternalSysmondIdAsync(sysmondActId, cancellationToken);
        if (localAct is not null && localAct.CompanyId != company.Id)
            throw new InvalidOperationException("Act farklı şirkete ait.");

        await _actQuery.DeleteActAsync(accessToken, sysmondActId, cancellationToken);

        if (localAct is null)
            return;

        var addresses = await _actAddressRepository.GetByActIdAsync(localAct.Id, cancellationToken);
        foreach (var address in addresses)
            _actAddressRepository.Remove(address);

        _actRepository.Remove(localAct);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteWarehouseAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondWarehouseId,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        if (sysmondWarehouseId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("id", "Sysmond warehouse id zorunludur.")]);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var localWarehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(sysmondWarehouseId, cancellationToken)
            ?? await _warehouseRepository.GetByIdAsync(sysmondWarehouseId, cancellationToken);
        if (localWarehouse is not null && localWarehouse.CompanyId != company.Id)
            throw new InvalidOperationException("Warehouse farklı şirkete ait.");

        await _inventoryQuery.DeleteWarehouseAsync(accessToken, sysmondWarehouseId, cancellationToken);

        if (localWarehouse is null)
            return;

        var localInventories = await _inventoryRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        foreach (var inv in localInventories.Where(i => i.WarehouseId == localWarehouse.Id))
            _inventoryRepository.Remove(inv);

        _warehouseRepository.Remove(localWarehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteIncomingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        CancellationToken cancellationToken = default)
    {
        await DeleteDespatchInternalAsync(
            sysmondCompanyId,
            accessToken,
            sysmondDespatchId,
            incoming: true,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOutgoingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        CancellationToken cancellationToken = default)
    {
        await DeleteDespatchInternalAsync(
            sysmondCompanyId,
            accessToken,
            sysmondDespatchId,
            incoming: false,
            cancellationToken);
    }

    private async Task DeleteDespatchInternalAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        bool incoming,
        CancellationToken cancellationToken)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        if (sysmondDespatchId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("id", "Sysmond despatch id zorunludur.")]);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var localPo = await _purchaseOrderRepository.GetByExternalSysmondIdWithItemsAsync(sysmondDespatchId, cancellationToken);
        if (localPo is not null && localPo.CompanyId != company.Id)
            throw new InvalidOperationException("Despatch farklı şirkete ait.");

        if (incoming)
            await _despatchCommand.DeleteIncomingDraftAsync(accessToken, sysmondDespatchId, cancellationToken);
        else
            await _despatchCommand.DeleteOutgoingDraftAsync(accessToken, sysmondDespatchId, cancellationToken);

        if (localPo is null)
            return;

        _purchaseOrderRepository.Remove(localPo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
