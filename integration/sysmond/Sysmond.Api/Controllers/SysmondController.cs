using Core.Validations;
using FluentValidation.Results;
using Integration.Sysmond.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Integration.Sysmond.Api.Controllers;

/// <summary>
/// Sysmondax manuel senkron endpoint'leri.
/// Create/Update/Delete için ana domain endpoint'lerini kullanın
/// (Products, Warehouses, Acts, PurchaseOrders/despatches) — tek istekte lokal + Sysmond.
/// Bu controller yalnızca sync/debug içindir; Bearer = Sysmondax access_token.
/// </summary>
[ApiController]
[Route("api/sysmond")]
[AllowAnonymous]
public class SysmondController : ControllerBase
{
    private readonly ISysmondSyncService _syncService;
    private readonly ISysmondActQueryService _actQuery;

    public SysmondController(
        ISysmondSyncService syncService,
        ISysmondActQueryService actQuery)
    {
        _syncService = syncService;
        _actQuery = actQuery;
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
    /// Cari (act-query) + adres (act-address) → Act / ActAddress upsert; orphan silme.
    /// Örnek: POST /api/sysmond/sync/acts?companyId={sysmondCompanyGuid}
    /// </summary>
    [HttpPost("sync/acts")]
    public async Task<IActionResult> SyncActs(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var result = await _syncService.SyncActsAsync(cid, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Stock-receipt (giriş/çıkış/transfer) → stock_transactions upsert.
    /// Sysmond UI: /stocks/entry, /stocks/exit, /stocks/transfer.
    /// Inventory miktarı değişmez (balance sync ayrı).
    /// Örnek: POST /api/sysmond/sync/stock-transactions?companyId={sysmondCompanyGuid}
    /// </summary>
    [HttpPost("sync/stock-transactions")]
    public async Task<IActionResult> SyncStockTransactions(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var (cid, token) = RequireCompanyAndBearer(companyId);
        var result = await _syncService.SyncStockTransactionsAsync(cid, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// act-address ham yanıt + parse sonucu (tanılama).
    /// Örnek: GET /api/sysmond/debug/act-address?companyId={guid}&amp;actId={actGuid}
    /// </summary>
    [HttpGet("debug/act-address")]
    public async Task<IActionResult> DebugActAddress(
        [FromQuery] Guid companyId,
        [FromQuery] Guid actId,
        CancellationToken cancellationToken = default)
    {
        var (_, token) = RequireCompanyAndBearer(companyId);
        if (actId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(actId), "actId zorunludur.")
            ]);
        }

        var result = await _actQuery.GetActAddressesDebugAsync(
            token,
            actId,
            companyId,
            includeDisabled: false,
            cancellationToken);
        return Ok(result);
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
