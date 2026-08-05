using Core.DTOs.StockTransfers;

namespace Core.Services;

/// <summary>StockTransfer iş kuralları (DTO tabanlı).</summary>
public interface IStockTransferService
{
    Task<StockTransferResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransferResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StockTransferResponse> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
