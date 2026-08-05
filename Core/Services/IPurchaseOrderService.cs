using Core.Entities;

namespace Core.Services;

/// <summary>PurchaseOrder iş kuralları; sipariş oluşturma, onaylama ve mal kabulü.</summary>
public interface IPurchaseOrderService : IGenericService<PurchaseOrder>
{
    Task<IReadOnlyList<PurchaseOrder>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReceiveAsync(Guid id, IDictionary<Guid, int> receivedQuantities, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
