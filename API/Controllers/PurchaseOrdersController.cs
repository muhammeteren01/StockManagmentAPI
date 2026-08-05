using Core.DTOs.PurchaseOrders;
using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Satın alma siparişi endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    /// <summary>Tüm siparişleri listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrder>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile sipariş getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrder>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Şirkete ait siparişleri listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrder>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    /// <summary>Yeni sipariş oluşturur (Pending).</summary>
    [HttpPost]
    public async Task<ActionResult<PurchaseOrder>> Create([FromBody] PurchaseOrder order, CancellationToken cancellationToken)
    {
        var created = await _purchaseOrderService.CreateAsync(order, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Siparişi onaylar (Approved).</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.ApproveAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Mal kabulü yapar; stok girişi (IN) oluşur.</summary>
    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.ReceiveAsync(id, request.ReceivedQuantities, cancellationToken);
        return NoContent();
    }

    /// <summary>Siparişi iptal eder.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
