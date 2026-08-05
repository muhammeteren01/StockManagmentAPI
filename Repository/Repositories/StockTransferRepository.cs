using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>StockTransfer veri erişim implementasyonu.</summary>
public class StockTransferRepository : GenericRepository<StockTransfer>, IStockTransferRepository
{
    public StockTransferRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StockTransfer?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<StockTransfer?> GetByReferenceNoAsync(string referenceNo, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReferenceNo == referenceNo, cancellationToken);
    }
}
