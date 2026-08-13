using Integration.Sysmond.Core.DTOs;



namespace Integration.Sysmond.Core.Services;



/// <summary>Sysmondax stock-query HTTP istemcisi.</summary>

public interface ISysmondStockQueryService

{

    /// <summary>

    /// <c>GET /api/app/stock-query</c> tüm sayfaları çeker (IncludePrice=true).

    /// </summary>

    /// <param name="accessToken">Sysmondax Bearer access_token.</param>

    /// <param name="companyId">Opsiyonel Sysmond company filtresi.</param>

    Task<IReadOnlyList<SysmondStockDto>> GetAllStocksAsync(

        string accessToken,

        Guid? companyId = null,

        CancellationToken cancellationToken = default);

}


