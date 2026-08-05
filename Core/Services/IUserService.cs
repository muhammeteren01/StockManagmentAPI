using Core.Entities;

namespace Core.Services;

/// <summary>User iş kuralları; kayıt, yetki ve şirket bazlı kullanıcı yönetimi.</summary>
public interface IUserService : IGenericService<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
