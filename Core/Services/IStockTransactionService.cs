using Core.Entities;

namespace Core.Services;

/// <summary>StockTransaction iş kuralları; stok hareketi oluşturur ve Inventory'yi günceller.</summary>
public interface IStockTransactionService : IGenericService<StockTransaction>
{
    Task<IReadOnlyList<StockTransaction>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
