using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>PurchaseOrder veri erişim implementasyonu.</summary>
public class PurchaseOrderRepository : GenericRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PurchaseOrder?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
