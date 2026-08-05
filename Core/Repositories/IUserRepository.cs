using Core.Entities;

namespace Core.Repositories;

/// <summary>User entity'sine ait veri erişim işlemleri.</summary>
public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
