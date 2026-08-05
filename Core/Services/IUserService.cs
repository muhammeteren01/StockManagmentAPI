using Core.DTOs.Users;

namespace Core.Services;

/// <summary>User iş kuralları (DTO tabanlı).</summary>
public interface IUserService
{
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
