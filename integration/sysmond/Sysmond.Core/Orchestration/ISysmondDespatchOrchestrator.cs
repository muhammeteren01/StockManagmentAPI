using Core.DTOs.PurchaseOrders;
using Integration.Sysmond.Core.DTOs;

namespace Integration.Sysmond.Core.Orchestration;

/// <summary>Tek istek: lokal PurchaseOrder + Sysmond despatch yazma.</summary>
public interface ISysmondDespatchOrchestrator
{
    Task<PurchaseOrderResponse> CreateIncomingAsync(
        Guid companyId,
        SysmondCreateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse> CreateOutgoingAsync(
        Guid companyId,
        SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse> UpdateIncomingAsync(
        Guid purchaseOrderId,
        SysmondUpdateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse> UpdateOutgoingAsync(
        Guid purchaseOrderId,
        SysmondUpdateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteIncomingAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);

    Task DeleteOutgoingAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
}
