using Core.Entities;

namespace Core.Repositories;

/// <summary>PurchaseOrder entity'sine ait veri erişim işlemleri (Items ile birlikte).</summary>
public interface IPurchaseOrderRepository : IGenericRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
