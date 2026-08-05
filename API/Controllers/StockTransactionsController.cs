using Core.Authorization;
using Core.DTOs.StockTransactions;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Stok hareketi endpoint'leri.</summary>
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

    /// <summary>Yeni stok hareketi oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.StockOps)]
    public async Task<ActionResult<StockTransactionResponse>> Create([FromBody] CreateStockTransactionRequest request, CancellationToken cancellationToken)
    {
        var created = await _stockTransactionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
