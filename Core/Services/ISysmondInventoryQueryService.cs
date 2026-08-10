using Core.DTOs.Sysmond;

namespace Core.Services;

/// <summary>Sysmondax warehouse / warehouse-stock / stock-balance HTTP istemcisi.</summary>
public interface ISysmondInventoryQueryService
{
    /// <summary><c>GET /api/app/warehouse?companyId=</c></summary>
    Task<IReadOnlyList<SysmondWarehouseDto>> GetWarehousesAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/warehouse-stock?CompanyId=</c>.
    /// Miktar yok; Inventory.ExternalSysmondId için satır id'si.
    /// 403 vb. çağıran katmanda yakalanabilir.
    /// </summary>
    Task<IReadOnlyList<SysmondWarehouseStockDto>> GetWarehouseStocksAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/user-profile/my-company-periods</c>.
    /// stock/balance için zorunlu CompanyPeriodId buradan seçilir.
    /// </summary>
    Task<IReadOnlyList<SysmondCompanyPeriodDto>> GetMyCompanyPeriodsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/stock/balance?CompanyPeriodId=&amp;StockId=</c> tüm sayfalar
    /// (depo kırılımlı <see cref="SysmondStockBalanceDto.Rem"/>).
    /// Not: yalnızca WarehouseId ile çağrı sandbox'ta boş dönebiliyor; StockId ile çağır.
    /// </summary>
    Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesByStockAsync(
        string accessToken,
        Guid companyPeriodId,
        Guid stockId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/stock/balance?CompanyPeriodId=&amp;WarehouseId=</c> tüm sayfalar.
    /// </summary>
    Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesByWarehouseAsync(
        string accessToken,
        Guid companyPeriodId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);
}
