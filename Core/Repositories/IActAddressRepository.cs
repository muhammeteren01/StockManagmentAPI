using Core.Entities;

namespace Core.Repositories;

/// <summary>Sysmond-synced ActAddress veri erişimi.</summary>
public interface IActAddressRepository : IGenericRepository<ActAddress>
{
    Task<ActAddress?> GetByExternalSysmondIdAsync(
        Guid externalSysmondId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActAddress>> GetByActIdAsync(Guid actId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActAddress>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
