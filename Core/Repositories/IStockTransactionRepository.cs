using Core.Entities;
using Core.Enums;

namespace Core.Repositories;

/// <summary>StockTransaction entity'sine ait veri erişim işlemleri.</summary>
public interface IStockTransactionRepository : IGenericRepository<StockTransaction>
{
    Task<IReadOnlyList<StockTransaction>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransaction>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransaction>> GetByTypeAsync(TransactionType type, CancellationToken cancellationToken = default);
    Task<StockTransaction?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sysmond stock-receipt sync orphan: ExternalSysmondId dolu + dönem eşleşen hareketler.
    /// </summary>
    Task<IReadOnlyList<StockTransaction>> GetSysmondByCompanyPeriodAsync(
        Guid companyId,
        Guid companyPeriodId,
        CancellationToken cancellationToken = default);
}
