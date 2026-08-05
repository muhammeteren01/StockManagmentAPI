using Core.DTOs.StockTransactions;

namespace Core.Services;

/// <summary>StockTransaction iş kuralları (DTO tabanlı).</summary>
public interface IStockTransactionService
{
    Task<StockTransactionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransactionResponse>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransactionResponse>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<StockTransactionResponse> CreateAsync(CreateStockTransactionRequest request, CancellationToken cancellationToken = default);
}
