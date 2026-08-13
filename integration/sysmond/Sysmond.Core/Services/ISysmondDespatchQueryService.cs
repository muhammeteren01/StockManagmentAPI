using Integration.Sysmond.Core.DTOs;

namespace Integration.Sysmond.Core.Services;

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

    /// <summary>
    /// <c>GET /api/app/despatch-query/{despatchId}/despatch-delivery-address</c>.
    /// Adres yoksa <c>null</c> (404 / Sysmond 403 “bulunamadı” soft).
    /// </summary>
    Task<SysmondDespatchDeliveryAddressDto?> GetDespatchDeliveryAddressAsync(
        string accessToken,
        Guid despatchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/company-address/{addressId}/address-by-id</c> — irsaliyedeki şirket adresi.
    /// Bulunamazsa <c>null</c>.
    /// </summary>
    Task<SysmondCompanyAddressDto?> GetCompanyAddressByIdAsync(
        string accessToken,
        Guid companyAddressId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/despatch-party?despatchId=&amp;companyId=</c> — cari/taraf + gömülü adres.
    /// </summary>
    Task<IReadOnlyList<SysmondDespatchPartyDto>> GetDespatchPartiesAsync(
        string accessToken,
        Guid companyId,
        Guid despatchId,
        CancellationToken cancellationToken = default);
}
