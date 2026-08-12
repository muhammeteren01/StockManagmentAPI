using Core.Entities;

namespace Core.Repositories;

/// <summary>Sysmond-synced Act (cari) veri erişimi.</summary>
public interface IActRepository : IGenericRepository<Act>
{
    Task<Act?> GetByExternalSysmondIdAsync(Guid externalSysmondId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Act>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Act>> GetByCompanyIdWithAddressesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
