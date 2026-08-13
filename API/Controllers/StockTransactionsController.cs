using Core.Authorization;
using Core.DTOs.StockTransactions;
using Core.Enums;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Stok hareketleri (giriş / çıkış / transfer / düzeltme).
/// Sysmond fişleri: POST /api/sysmond/sync/stock-transactions ile buraya akar.
/// Depolar arası lokal transfer yaşam döngüsü: /api/StockTransfers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockTransactionsController : ControllerBase
{
    private readonly IStockTransactionService _stockTransactionService;

    public StockTransactionsController(IStockTransactionService stockTransactionService)
        => _stockTransactionService = stockTransactionService;

    /// <summary>Tüm stok hareketlerini listeler.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _stockTransactionService.GetAllAsync(cancellationToken));

    /// <summary>Stok girişleri (TransactionType=In).</summary>
    [HttpGet("entries")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>> GetEntries(CancellationToken cancellationToken)
        => Ok(await _stockTransactionService.GetByTypeAsync(TransactionType.In, cancellationToken));

    /// <summary>Stok çıkışları (TransactionType=Out).</summary>
    [HttpGet("exits")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>> GetExits(CancellationToken cancellationToken)
        => Ok(await _stockTransactionService.GetByTypeAsync(TransactionType.Out, cancellationToken));

    /// <summary>Transfer hareketleri (TransferOut + TransferIn).</summary>
    [HttpGet("transfers")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>> GetTransfers(CancellationToken cancellationToken)
    {
        var outs = await _stockTransactionService.GetByTypeAsync(TransactionType.TransferOut, cancellationToken);
        var ins = await _stockTransactionService.GetByTypeAsync(TransactionType.TransferIn, cancellationToken);
        var merged = outs.Concat(ins).OrderByDescending(x => x.TransactionDate).ToList();
        return Ok(merged);
    }

    /// <summary>Id ile stok hareketi getirir.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<StockTransactionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _stockTransactionService.GetByIdAsync(id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    /// <summary>Ürüne ait hareketleri listeler.</summary>
    [HttpGet("by-product/{productId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>> GetByProduct(Guid productId, CancellationToken cancellationToken)
        => Ok(await _stockTransactionService.GetByProductIdAsync(productId, cancellationToken));

    /// <summary>Depoya ait hareketleri listeler.</summary>
    [HttpGet("by-warehouse/{warehouseId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>> GetByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
        => Ok(await _stockTransactionService.GetByWarehouseIdAsync(warehouseId, cancellationToken));

    /// <summary>
    /// Yeni stok hareketi oluşturur (lokal).
    /// In=giriş, Out=çıkış; Inventory miktarı güncellenir.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.StockOps)]
    public async Task<ActionResult<StockTransactionResponse>> Create([FromBody] CreateStockTransactionRequest request, CancellationToken cancellationToken)
    {
        var created = await _stockTransactionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
