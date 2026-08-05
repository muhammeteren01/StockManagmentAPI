using Core.DTOs.PurchaseOrders;

namespace Core.Services;

/// <summary>PurchaseOrder iş kuralları (DTO tabanlı).</summary>
public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrderResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReceiveAsync(Guid id, IDictionary<Guid, int> receivedQuantities, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
