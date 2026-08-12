using Core.DTOs.PurchaseOrders;
using Core.DTOs.Sysmond;
using Core.DTOs.Warehouses;
using Core.Entities;
using Core.Enums;
using Core.Mappings;
using Core.Validations;
using FluentValidation.Results;

namespace Service.Services.Sysmond;

public partial class SysmondSyncService
{
    /// <inheritdoc />
    public async Task<SysmondActResponse> UpdateActAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondActId,
        SysmondUpdateActRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        if (sysmondActId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("id", "Sysmond act id zorunludur.")]);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var act = await _actRepository.GetByExternalSysmondIdAsync(sysmondActId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Act bulunamadı (ExternalSysmondId={sysmondActId}). Önce sync yapın.");

        if (act.CompanyId != company.Id)
            throw new InvalidOperationException("Act farklı şirkete ait.");

        var body = SysmondActMapper.ToActUpdateDto(act, request, company.Id, sysmondActId);
        await _actQuery.UpdateActAsync(accessToken, body, cancellationToken);

        SysmondActMapper.ApplyUpdateFromRequest(act, request);
        _actRepository.Update(act);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SysmondActMapper.ToResponse(act);
    }

    /// <inheritdoc />
    public async Task<WarehouseResponse> UpdateWarehouseAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondWarehouseId,
        SysmondUpdateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        if (sysmondWarehouseId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("id", "Sysmond warehouse id zorunludur.")]);

        if (string.IsNullOrWhiteSpace(request.Name) && request.WarehouseCode is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request), "En az name veya warehouseCode güncellenmelidir.")
            ]);
        }

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(sysmondWarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Warehouse bulunamadı (ExternalSysmondId={sysmondWarehouseId}). Önce sync yapın.");

        if (warehouse.CompanyId != company.Id)
            throw new InvalidOperationException("Warehouse farklı şirkete ait.");

        var body = SysmondInventoryMapper.ToWarehouseUpdateDto(
            warehouse, request, company.Id, sysmondWarehouseId);
        await _inventoryQuery.UpdateWarehouseAsync(accessToken, body, cancellationToken);

        SysmondInventoryMapper.ApplyUpdateFromRequest(warehouse, request);
        _warehouseRepository.Update(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return WarehouseMapper.ToResponse(warehouse);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> UpdateIncomingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        SysmondUpdateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        var order = await UpdateDespatchInternalAsync(
            sysmondCompanyId,
            accessToken,
            sysmondDespatchId,
            incoming: true,
            async (company, localPo) =>
            {
                var body = BuildIncomingDespatchUpdateDto(sysmondDespatchId, localPo, request);
                await _despatchCommand.UpdateIncomingDraftAsync(accessToken, body, cancellationToken);
                ApplyIncomingDespatchUpdateToOrder(localPo, request);
            },
            cancellationToken);

        return PurchaseOrderMapper.ToResponse(order);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> UpdateOutgoingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        SysmondUpdateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        var order = await UpdateDespatchInternalAsync(
            sysmondCompanyId,
            accessToken,
            sysmondDespatchId,
            incoming: false,
            async (company, localPo) =>
            {
                var body = BuildOutgoingDespatchUpdateDto(sysmondDespatchId, localPo, request);
                await _despatchCommand.UpdateOutgoingDraftAsync(accessToken, body, cancellationToken);
                ApplyOutgoingDespatchUpdateToOrder(localPo, request);
            },
            cancellationToken);

        return PurchaseOrderMapper.ToResponse(order);
    }

    private async Task<PurchaseOrder> UpdateDespatchInternalAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        bool incoming,
        Func<Company, PurchaseOrder, Task> updateRemoteAndLocal,
        CancellationToken cancellationToken)
    {
        if (sysmondDespatchId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("id", "Sysmond despatch id zorunludur.")]);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var localPo = await _purchaseOrderRepository.GetByExternalSysmondIdWithItemsAsync(
            sysmondDespatchId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PurchaseOrder bulunamadı (ExternalSysmondId={sysmondDespatchId}). Önce create/sync yapın.");

        if (localPo.CompanyId != company.Id)
            throw new InvalidOperationException("Despatch farklı şirkete ait.");

        var expectedDirection = incoming
            ? DespatchDirection.Incoming
            : DespatchDirection.Outgoing;
        if (localPo.Direction != expectedDirection)
        {
            throw new InvalidOperationException(
                incoming
                    ? "Belge giden irsaliye; incoming update kullanılamaz."
                    : "Belge gelen irsaliye; outgoing update kullanılamaz.");
        }

        await updateRemoteAndLocal(company, localPo);
        _purchaseOrderRepository.Update(localPo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return localPo;
    }

    private static SysmondIncomingDespatchUpdateDto BuildIncomingDespatchUpdateDto(
        Guid despatchId,
        PurchaseOrder localPo,
        SysmondUpdateIncomingDespatchRequest request)
    {
        var companyPeriodId = request.CompanyPeriodId ?? localPo.ExternalSysmondCompanyPeriodId;
        if (companyPeriodId is null || companyPeriodId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.CompanyPeriodId),
                    "companyPeriodId zorunludur (istekte veya yerel kayıtta).")
            ]);
        }

        var issueDate = request.IssueDate ?? localPo.IssueDate ?? DateTime.UtcNow;
        var actualDate = request.ActualDespatchDate ?? localPo.ActualDespatchDate ?? issueDate;

        var deliveryUpdate = request.DeliveryAddressUpdate;
        if (deliveryUpdate is not null)
        {
            deliveryUpdate.DespatchId ??= despatchId;
        }

        return new SysmondIncomingDespatchUpdateDto
        {
            Id = despatchId,
            CompanyPeriodId = companyPeriodId.Value,
            Scenario = request.Scenario ?? 30,
            Type = request.Type ?? 10,
            DocNo = request.DocNo,
            IssueDate = issueDate,
            ActualDespatchDate = actualDate,
            CarrierId = request.CarrierId,
            Description = request.Description,
            CurrencyId = request.CurrencyId ?? 949,
            CurrencyExchangeRate = request.CurrencyExchangeRate is > 0
                ? request.CurrencyExchangeRate.Value
                : 1,
            IdisShipmentNo = request.IdisShipmentNo,
            DeliveryAddressUpdateDto = deliveryUpdate
        };
    }

    private static SysmondOutgoingDespatchUpdateDto BuildOutgoingDespatchUpdateDto(
        Guid despatchId,
        PurchaseOrder localPo,
        SysmondUpdateOutgoingDespatchRequest request)
    {
        var companyPeriodId = request.CompanyPeriodId ?? localPo.ExternalSysmondCompanyPeriodId;
        if (companyPeriodId is null || companyPeriodId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.CompanyPeriodId),
                    "companyPeriodId zorunludur (istekte veya yerel kayıtta).")
            ]);
        }

        var issueDate = request.IssueDate ?? localPo.IssueDate ?? DateTime.UtcNow;
        var actualDate = request.ActualDespatchDate ?? localPo.ActualDespatchDate ?? issueDate;

        var deliveryUpdate = request.DeliveryAddressUpdate;
        if (deliveryUpdate is not null)
        {
            deliveryUpdate.DespatchId ??= despatchId;
        }

        return new SysmondOutgoingDespatchUpdateDto
        {
            Id = despatchId,
            CompanyPeriodId = companyPeriodId.Value,
            Scenario = request.Scenario ?? 30,
            Type = request.Type ?? 10,
            TemplateId = request.TemplateId,
            IssueDate = issueDate,
            ActualDespatchDate = actualDate,
            CarrierId = request.CarrierId,
            Description = request.Description,
            CurrencyId = request.CurrencyId ?? 949,
            CurrencyExchangeRate = request.CurrencyExchangeRate is > 0
                ? request.CurrencyExchangeRate.Value
                : 1,
            ReceiverPkAlias = request.ReceiverPkAlias,
            IdisShipmentNo = request.IdisShipmentNo,
            DeliveryAddressUpdateDto = deliveryUpdate
        };
    }

    private static void ApplyIncomingDespatchUpdateToOrder(
        PurchaseOrder order,
        SysmondUpdateIncomingDespatchRequest request)
    {
        if (request.CompanyPeriodId is Guid period && period != Guid.Empty)
            order.ExternalSysmondCompanyPeriodId = period;
        if (!string.IsNullOrWhiteSpace(request.DocNo))
            order.OrderNumber = request.DocNo.Trim();
        if (request.IssueDate is not null)
            order.IssueDate = request.IssueDate;
        if (request.ActualDespatchDate is not null)
            order.ActualDespatchDate = request.ActualDespatchDate;
        if (request.DeliveryAddressUpdate is not null)
            SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, request.DeliveryAddressUpdate);
    }

    private static void ApplyOutgoingDespatchUpdateToOrder(
        PurchaseOrder order,
        SysmondUpdateOutgoingDespatchRequest request)
    {
        if (request.CompanyPeriodId is Guid period && period != Guid.Empty)
            order.ExternalSysmondCompanyPeriodId = period;
        if (request.IssueDate is not null)
            order.IssueDate = request.IssueDate;
        if (request.ActualDespatchDate is not null)
            order.ActualDespatchDate = request.ActualDespatchDate;
        if (request.DeliveryAddressUpdate is not null)
            SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, request.DeliveryAddressUpdate);
    }
}
