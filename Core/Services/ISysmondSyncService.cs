using Core.DTOs.Products;
using Core.DTOs.PurchaseOrders;
using Core.DTOs.Sysmond;
using Core.DTOs.Warehouses;

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
    /// Despatch-query → PurchaseOrder + Item upsert; dönem-scoped orphan (stok hareketi yok).
    /// companyAddressId + adres JSON: teslimat → cari (despatch-party) → şirket adresi.
    /// Product/Warehouse ExternalSysmondId ile eşlenir. Outbound create yok.
    /// </summary>
    Task<SysmondDespatchSyncResult> SyncDespatchesAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Act-query → Act upsert; her cari için act-address → ActAddress upsert; orphan silme.
    /// Types: 10/20/30/40. Outbound create yok.
    /// </summary>
    Task<SysmondActSyncResult> SyncActsAsync(
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

    /// <summary>Sysmondax <c>PUT /api/app/act/act</c> + yerel Act güncelleme.</summary>
    Task<SysmondActResponse> UpdateActAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondActId,
        SysmondUpdateActRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmondax <c>PUT /api/app/warehouse</c> + yerel Warehouse güncelleme.</summary>
    Task<WarehouseResponse> UpdateWarehouseAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondWarehouseId,
        SysmondUpdateWarehouseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmond incoming draft günceller + yerel PurchaseOrder.</summary>
    Task<PurchaseOrderResponse> UpdateIncomingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        SysmondUpdateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmond outgoing draft günceller + yerel PurchaseOrder.</summary>
    Task<PurchaseOrderResponse> UpdateOutgoingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        SysmondUpdateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmond act siler + yerelde Act/ActAddress temizler.</summary>
    Task DeleteActAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondActId,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmond warehouse siler + yerelde Warehouse/Inventory temizler.</summary>
    Task DeleteWarehouseAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondWarehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmond incoming draft irsaliyeyi siler + yerel PurchaseOrder temizler.</summary>
    Task DeleteIncomingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        CancellationToken cancellationToken = default);

    /// <summary>Sysmond outgoing draft irsaliyeyi siler + yerel PurchaseOrder temizler.</summary>
    Task DeleteOutgoingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondDespatchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gelen irsaliye: Sysmond draft → item(s) → save, sonra lokal PurchaseOrder/Item yazar.
    /// companyPeriodId boşsa aktif dönem seçilir. Stok hareketi yazılmaz.
    /// </summary>
    Task<PurchaseOrderResponse> CreateIncomingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Giden irsaliye: Sysmond outgoing draft → item(s) → save + lokal PO.
    /// companyAddressId zorunlu; period boşsa aktif dönem. Stok hareketi yok.
    /// </summary>
    Task<PurchaseOrderResponse> CreateOutgoingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default);
}
