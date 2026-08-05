using Core.DTOs.Companies;

namespace Core.Services;

/// <summary>Company iş kuralları (DTO tabanlı).</summary>
public interface ICompanyService
{
    Task<CompanyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
