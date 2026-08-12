using Core.DTOs.Sysmond;

namespace Core.Services;

/// <summary>Sysmondax act-query + act-address HTTP istemcisi.</summary>
public interface ISysmondActQueryService
{
    /// <summary>
    /// <c>GET /api/app/act-query</c> tüm sayfaları çeker.
    /// <paramref name="types"/> null/boşsa tüm tipler; doluysa Types query ile filtreler.
    /// </summary>
    Task<IReadOnlyList<SysmondActDto>> GetAllActsAsync(
        string accessToken,
        Guid companyId,
        IReadOnlyList<int>? types = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/act-address?actId=</c>.
    /// Adres yoksa boş liste; 403+Error:50001 soft-empty.
    /// </summary>
    Task<IReadOnlyList<SysmondActAddressDto>> GetActAddressesAsync(
        string accessToken,
        Guid actId,
        Guid companyId,
        bool includeDisabled = false,
        CancellationToken cancellationToken = default);

    /// <summary>act-address ham yanıt + parse (tanılama).</summary>
    Task<SysmondActAddressDebugResult> GetActAddressesDebugAsync(
        string accessToken,
        Guid actId,
        Guid companyId,
        bool includeDisabled = true,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET /api/app/act-query/{id}/by-id</c></summary>
    Task<SysmondActDto?> GetActByIdAsync(
        string accessToken,
        Guid actId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/act-query/{actId}/despatch-scenarios-by-act-id</c>.
    /// </summary>
    Task<IReadOnlyList<SysmondDespatchScenarioTypeMapDto>> GetDespatchScenariosByActIdAsync(
        string accessToken,
        Guid actId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/company-invoice-template?CompanyId=</c> (sayfalı; irsaliye type 50 dahil).
    /// </summary>
    Task<IReadOnlyList<SysmondCompanyDocNoTemplateDto>> GetCompanyDocNoTemplatesAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/app/act/act</c> yeni cari oluşturur.</summary>
    Task<Guid> CreateActAsync(
        string accessToken,
        SysmondActCreateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT /api/app/act/act</c> cari günceller.</summary>
    Task UpdateActAsync(
        string accessToken,
        SysmondActUpdateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>DELETE /api/app/act/{id}/act</c> cari siler.</summary>
    Task DeleteActAsync(
        string accessToken,
        Guid actId,
        CancellationToken cancellationToken = default);
}
