using Core.DTOs.Sysmond;

namespace Core.Services;

/// <summary>Sysmondax despatch-query (irsaliye) HTTP istemcisi.</summary>
public interface ISysmondDespatchQueryService
{
    /// <summary>
    /// <c>GET /api/app/despatch-query/despatches</c> tüm sayfalar.
    /// Yalnızca CompanyId (CompanyPeriodId / IsDraft gönderilmez — tüm dönem ve durumlar).
    /// </summary>
    Task<IReadOnlyList<SysmondDespatchDto>> GetDespatchesAsync(
        string accessToken,
        Guid companyId,
        Guid? companyPeriodId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/despatch-query/{despatchId}/despatch-items</c>.
    /// </summary>
    Task<IReadOnlyList<SysmondDespatchItemDto>> GetDespatchItemsAsync(
        string accessToken,
        Guid despatchId,
        CancellationToken cancellationToken = default);
}
