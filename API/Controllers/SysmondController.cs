using Core.DTOs.Sysmond;
using Core.Services;
using Core.Validations;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace API.Controllers;

/// <summary>
/// Sysmondax manuel senkron endpoint'leri (background worker yok).
/// Yerel JWT / SuperAdmin gerekmez — Swagger Authorize'a Sysmondax access_token yazılır;
/// bu Bearer Sysmondax API çağrılarına iletilir.
/// </summary>
[ApiController]
[Route("api/sysmond")]
[AllowAnonymous]
public class SysmondController : ControllerBase
{
    private readonly ISysmondSyncService _syncService;

    public SysmondController(ISysmondSyncService syncService)
    {
        _syncService = syncService;
    }

    /// <summary>
    /// Sysmond stock-query ürünlerini çeker ve yerel Product'lara upsert eder.
    /// Örnek: POST /api/sysmond/sync/products?companyId={sysmondCompanyGuid}
    /// </summary>
    [HttpPost("sync/products")]
    public async Task<IActionResult> SyncProducts(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var result = await _syncService.SyncProductsAsync(cid, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Warehouse + stock/balance → Inventory upsert / orphan delete.
    /// Örnek: POST /api/sysmond/sync/inventories?companyId={sysmondCompanyGuid}
    /// </summary>
    [HttpPost("sync/inventories")]
    public async Task<IActionResult> SyncInventories(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var result = await _syncService.SyncInventoriesAsync(cid, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Ürün senkronu, ardından inventory senkronu.
    /// Örnek: POST /api/sysmond/sync?companyId={sysmondCompanyGuid}
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncAll(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var result = await _syncService.SyncAllAsync(cid, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// İrsaliye (despatch-query) → PurchaseOrder + PurchaseOrderItem upsert.
    /// companyAddressId + adres JSON (teslimat → cari party → şirket adresi) yazılır.
    /// Aktif CompanyPeriod ile çekilir; aynı dönemdeki remote'da olmayan belgeler silinir.
    /// Stok hareketi (StockTransaction) bu sync'te yazılmaz.
    /// Örnek: POST /api/sysmond/sync/despatches?companyId={sysmondCompanyGuid}
    /// </summary>
    [HttpPost("sync/despatches")]
    public async Task<IActionResult> SyncDespatches(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var result = await _syncService.SyncDespatchesAsync(cid, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sysmondax'a stok oluşturur (POST /api/app/stock) ve yerel Product (+ openingQuantity → Inventory) yazar.
    /// Örnek: POST /api/sysmond/stocks?companyId={guid}
    /// openingQuantity: [{ warehouseId, quantity }] — ilgili depoya açılış adedi.
    /// </summary>
    [HttpPost("stocks")]
    public async Task<IActionResult> CreateStock(
        [FromQuery] Guid companyId,
        [FromBody] SysmondCreateStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var created = await _syncService.CreateStockAsync(cid, token, request, cancellationToken);
        return Ok(created);
    }

    /// <summary>
    /// Sysmondax stok günceller (PUT /api/app/stock) ve yerel Product'ı günceller.
    /// id = Sysmond stock id (ExternalSysmondId). Örnek: PUT /api/sysmond/stocks/{id}?companyId={guid}
    /// </summary>
    [HttpPut("stocks/{id:guid}")]
    public async Task<IActionResult> UpdateStock(
        Guid id,
        [FromQuery] Guid companyId,
        [FromBody] SysmondUpdateStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var updated = await _syncService.UpdateStockAsync(cid, token, id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Sysmondax stok siler (DELETE /api/app/stock/{id}) ve yerel Product + Inventory'yi kaldırır.
    /// id = Sysmond stock id (ExternalSysmondId). Örnek: DELETE /api/sysmond/stocks/{id}?companyId={guid}
    /// </summary>
    [HttpDelete("stocks/{id:guid}")]
    public async Task<IActionResult> DeleteStock(
        Guid id,
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        await _syncService.DeleteStockAsync(cid, token, id, cancellationToken);
        return NoContent();
    }

    private (Guid CompanyId, string AccessToken) RequireCompanyAndBearer(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(companyId), "companyId zorunludur.")
            ]);
        }

        var accessToken = ExtractBearerToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure("Authorization", "Bearer Sysmondax access_token zorunludur.")
            ]);
        }

        return (companyId, accessToken);
    }

    /// <summary>Authorization header'dan Bearer token okur (Sysmondax access_token).</summary>
    private string? ExtractBearerToken()
    {
        var header = Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrWhiteSpace(header))
            return null;

        const string prefix = "Bearer ";
        if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return header[prefix.Length..].Trim();

        return null;
    }
}
