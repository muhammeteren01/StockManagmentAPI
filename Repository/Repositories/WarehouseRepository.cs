using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>Warehouse veri erişim implementasyonu.</summary>
public class WarehouseRepository : GenericRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Warehouse?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ExternalSysmondId == externalSysmondId, cancellationToken);
    }

    public async Task<IReadOnlyList<Warehouse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }
}
