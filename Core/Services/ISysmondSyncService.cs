using Core.DTOs.Products;
using Core.DTOs.Sysmond;

namespace Core.Services;

/// <summary>Sysmond → yerel ürün / inventory senkronu + Sysmondax'a stok oluşturma.</summary>
public interface ISysmondSyncService
{
    /// <summary>
    /// Stock-query'den ürünleri çeker; ExternalSysmondId veya (CompanyId, Sku) ile upsert eder.
    /// Remote set'te olmayan, ExternalSysmondId'li yerel ürünleri siler (Sysmond kaynak; local-only ürünler kalır).
    /// </summary>
    Task<SysmondProductSyncResult> SyncProductsAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Warehouse + stock/balance (miktar) → Inventory upsert; orphan silme.
    /// Warehouse-stock (varsa) ExternalSysmondId sağlar; 403 olursa soft-error ile devam.
    /// Ürünler önceden sync edilmiş olmalı (Product.ExternalSysmondId).
    /// </summary>
    Task<SysmondInventorySyncResult> SyncInventoriesAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>Önce ürün, sonra inventory senkronu.</summary>
    Task<SysmondFullSyncResult> SyncAllAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Despatch-query (irsaliye) → StockTransaction upsert + Inventory delta (orphan silme kapalı).
    /// Product/Warehouse ExternalSysmondId ile eşlenir. Outbound create yok.
    /// </summary>
    Task<SysmondDespatchSyncResult> SyncDespatchesAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sysmondax <c>POST /api/app/stock</c> + yerel Product (ve openingQuantity → Inventory).
    /// </summary>
    Task<ProductResponse> CreateStockAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateStockRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sysmondax <c>PUT /api/app/stock</c> + yerel Product güncelleme.
    /// <paramref name="sysmondStockId"/> = Product.ExternalSysmondId.
    /// </summary>
    Task<ProductResponse> UpdateStockAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondStockId,
        SysmondUpdateStockRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sysmondax <c>DELETE /api/app/stock/{id}</c> + yerel Product/Inventory silme.
    /// </summary>
    Task DeleteStockAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondStockId,
        CancellationToken cancellationToken = default);
}
