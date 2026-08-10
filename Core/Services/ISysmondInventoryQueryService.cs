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
    /// <c>GET /api/app/stock/balance?WarehouseId=</c> tüm sayfalar (<see cref="SysmondStockBalanceDto.Rem"/>).
    /// </summary>
    Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesByWarehouseAsync(
        string accessToken,
        Guid warehouseId,
        CancellationToken cancellationToken = default);
}
