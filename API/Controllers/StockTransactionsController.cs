using Core.Entities;
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
    {
        _stockTransactionService = stockTransactionService;
    }

    /// <summary>Tüm stok hareketlerini listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockTransaction>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _stockTransactionService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile stok hareketi getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockTransaction>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _stockTransactionService.GetByIdAsync(id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    /// <summary>Ürüne ait hareketleri listeler.</summary>
    [HttpGet("by-product/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<StockTransaction>>> GetByProduct(Guid productId, CancellationToken cancellationToken)
    {
        return Ok(await _stockTransactionService.GetByProductIdAsync(productId, cancellationToken));
    }

    /// <summary>Depoya ait hareketleri listeler.</summary>
    [HttpGet("by-warehouse/{warehouseId:guid}")]
    public async Task<ActionResult<IReadOnlyList<StockTransaction>>> GetByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
    {
        return Ok(await _stockTransactionService.GetByWarehouseIdAsync(warehouseId, cancellationToken));
    }

    /// <summary>Yeni stok hareketi oluşturur; Inventory güncellenir.</summary>
    [HttpPost]
    public async Task<ActionResult<StockTransaction>> Create([FromBody] StockTransaction transaction, CancellationToken cancellationToken)
    {
        var created = await _stockTransactionService.CreateAsync(transaction, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
