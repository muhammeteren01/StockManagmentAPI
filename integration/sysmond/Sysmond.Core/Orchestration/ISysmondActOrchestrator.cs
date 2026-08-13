using Core.DTOs.Acts;

namespace Integration.Sysmond.Core.Orchestration;

/// <summary>Tek istek: lokal Act + Sysmond act yazma.</summary>
public interface ISysmondActOrchestrator
{
    Task<ActResponse> CreateAsync(
        Guid companyId,
        CreateActRequest request,
        CancellationToken cancellationToken = default);

    Task<ActResponse> UpdateAsync(
        Guid actId,
        UpdateActRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid actId, CancellationToken cancellationToken = default);
}
