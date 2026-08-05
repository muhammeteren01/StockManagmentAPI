using Core.Authorization;
using Core.DTOs.StockTransfers;
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
        => _stockTransferService = stockTransferService;

    /// <summary>Tüm transferleri listeler.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<StockTransferResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _stockTransferService.GetAllAsync(cancellationToken));

    /// <summary>Id ile transfer getirir.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<StockTransferResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await _stockTransferService.GetByIdAsync(id, cancellationToken);
        return transfer is null ? NotFound() : Ok(transfer);
    }

    /// <summary>Yeni transfer oluşturur (Pending).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<StockTransferResponse>> Create([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        var created = await _stockTransferService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Transferi başlatır (InTransit).</summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        await _stockTransferService.StartAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Transferi tamamlar (Completed).</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _stockTransferService.CompleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Transferi iptal eder.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _stockTransferService.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
