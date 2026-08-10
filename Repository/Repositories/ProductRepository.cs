using Core.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace Repository.Repositories;

/// <summary>Product veri erişim implementasyonu.</summary>
public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetBySkuAsync(Guid companyId, string sku, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Sku == sku, cancellationToken);
    }

    public async Task<Product?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.ExternalSysmondId == externalSysmondId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }
}
