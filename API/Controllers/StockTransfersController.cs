using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Depolar arası transfer endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockTransfersController : ControllerBase
{
    private readonly IStockTransferService _stockTransferService;

    public StockTransfersController(IStockTransferService stockTransferService)
    {
        _stockTransferService = stockTransferService;
    }

    /// <summary>Tüm transferleri listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockTransfer>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _stockTransferService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile transfer getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockTransfer>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await _stockTransferService.GetByIdAsync(id, cancellationToken);
        return transfer is null ? NotFound() : Ok(transfer);
    }

    /// <summary>Yeni transfer oluşturur (Pending).</summary>
    [HttpPost]
    public async Task<ActionResult<StockTransfer>> Create([FromBody] StockTransfer transfer, CancellationToken cancellationToken)
    {
        var created = await _stockTransferService.CreateAsync(transfer, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Transferi başlatır (InTransit); kaynak depodan stok düşer.</summary>
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        await _stockTransferService.StartAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Transferi tamamlar (Completed); hedef depoya stok girer.</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _stockTransferService.CompleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Transferi iptal eder.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _stockTransferService.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
