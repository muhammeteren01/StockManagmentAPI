using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>StockTransaction veri erişim implementasyonu.</summary>
public class StockTransactionRepository : GenericRepository<StockTransaction>, IStockTransactionRepository
{
    public StockTransactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<StockTransaction>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransaction>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransaction>> GetByTypeAsync(
        TransactionType type,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.TransactionType == type)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockTransaction?> GetByExternalSysmondIdAsync(
        Guid externalSysmondId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            x => x.ExternalSysmondId == externalSysmondId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransaction>> GetSysmondByCompanyPeriodAsync(
        Guid companyId,
        Guid companyPeriodId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x =>
                x.CompanyId == companyId &&
                x.ExternalSysmondId != null &&
                x.ExternalSysmondCompanyPeriodId == companyPeriodId)
            .ToListAsync(cancellationToken);
    }
}
