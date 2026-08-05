using Core.Entities;

namespace Core.Repositories;

/// <summary>StockTransfer entity'sine ait veri erişim işlemleri (Items ile birlikte).</summary>
public interface IStockTransferRepository : IGenericRepository<StockTransfer>
{
    Task<StockTransfer?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockTransfer?> GetByReferenceNoAsync(string referenceNo, CancellationToken cancellationToken = default);
}
