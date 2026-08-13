using Core.DTOs.Acts;

namespace Core.Services;

/// <summary>Cari (Act) iş kuralları.</summary>
public interface IActService
{
    Task<ActResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<ActResponse> CreateAsync(CreateActRequest request, CancellationToken cancellationToken = default);
    Task<ActResponse> UpdateAsync(Guid id, UpdateActRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
