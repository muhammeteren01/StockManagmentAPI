using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>Inventory veri erişim implementasyonu.</summary>
public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
{
    public InventoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Inventory?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId, cancellationToken);
    }

    public async Task<Inventory?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ExternalSysmondId == externalSysmondId, cancellationToken);
    }

    public async Task<IReadOnlyList<Inventory>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Inventory>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Inventory>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }
}
