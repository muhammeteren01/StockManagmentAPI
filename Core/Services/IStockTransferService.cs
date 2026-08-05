using Core.Entities;

namespace Core.Services;

/// <summary>StockTransfer iş kuralları; depolar arası transfer oluşturma ve durum geçişleri.</summary>
public interface IStockTransferService : IGenericService<StockTransfer>
{
    Task StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
