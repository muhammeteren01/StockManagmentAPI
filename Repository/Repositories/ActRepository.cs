using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>Act (cari) veri erişim implementasyonu.</summary>
public class ActRepository : GenericRepository<Act>, IActRepository
{
    public ActRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Act?> GetByExternalSysmondIdAsync(
        Guid externalSysmondId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ExternalSysmondId == externalSysmondId, cancellationToken);
    }

    public async Task<IReadOnlyList<Act>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Act>> GetByCompanyIdWithAddressesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Include(x => x.Addresses)
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }
}
