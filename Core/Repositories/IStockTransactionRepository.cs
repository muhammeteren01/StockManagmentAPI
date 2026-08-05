using Core.Entities;

namespace Core.Repositories;

/// <summary>StockTransaction entity'sine ait veri erişim işlemleri.</summary>
public interface IStockTransactionRepository : IGenericRepository<StockTransaction>
{
    Task<IReadOnlyList<StockTransaction>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
