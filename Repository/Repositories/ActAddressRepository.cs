using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>ActAddress veri erişim implementasyonu.</summary>
public class ActAddressRepository : GenericRepository<ActAddress>, IActAddressRepository
{
    public ActAddressRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ActAddress?> GetByExternalSysmondIdAsync(
        Guid externalSysmondId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ExternalSysmondId == externalSysmondId, cancellationToken);
    }

    public async Task<IReadOnlyList<ActAddress>> GetByActIdAsync(
        Guid actId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.ActId == actId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActAddress>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }
}
