using Core.Authorization;
using Core.DTOs.PurchaseOrders;
using Core.Services;
using Integration.Sysmond.Core.DTOs;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

/// <summary>Satın alma siparişi + Sysmond irsaliye (tek istek) endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ISysmondDespatchOrchestrator _despatchOrchestrator;
    private readonly Core.Abstractions.ICurrentUser _currentUser;
    private readonly SysmondOptions _sysmondOptions;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        ISysmondDespatchOrchestrator despatchOrchestrator,
        Core.Abstractions.ICurrentUser currentUser,
        IOptions<SysmondOptions> sysmondOptions)
    {
        _purchaseOrderService = purchaseOrderService;
        _despatchOrchestrator = despatchOrchestrator;
        _currentUser = currentUser;
        _sysmondOptions = sysmondOptions.Value;
    }

    /// <summary>Tüm siparişleri listeler.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _purchaseOrderService.GetAllAsync(cancellationToken));

    /// <summary>Id ile sipariş getirir.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<PurchaseOrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Şirkete ait siparişleri listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderResponse>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
        => Ok(await _purchaseOrderService.GetByCompanyIdAsync(companyId, cancellationToken));

    /// <summary>Yeni sipariş oluşturur (Pending).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<PurchaseOrderResponse>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var created = await _purchaseOrderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Gelen irsaliye: tek istekte Sysmond + lokal PurchaseOrder.</summary>
    [HttpPost("despatches/incoming")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<PurchaseOrderResponse>> CreateIncomingDespatch(
        [FromBody] SysmondCreateIncomingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSysmondEnabled();
        var companyId = TenantGuard.ResolveCompanyId(_currentUser, null);
        var created = await _despatchOrchestrator.CreateIncomingAsync(companyId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Giden irsaliye: tek istekte Sysmond + lokal PurchaseOrder.</summary>
    [HttpPost("despatches/outgoing")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<PurchaseOrderResponse>> CreateOutgoingDespatch(
        [FromBody] SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSysmondEnabled();
        var companyId = TenantGuard.ResolveCompanyId(_currentUser, null);
        var created = await _despatchOrchestrator.CreateOutgoingAsync(companyId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Gelen irsaliye draft güncelle.</summary>
    [HttpPut("despatches/incoming/{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<PurchaseOrderResponse>> UpdateIncomingDespatch(
        Guid id,
        [FromBody] SysmondUpdateIncomingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSysmondEnabled();
        return Ok(await _despatchOrchestrator.UpdateIncomingAsync(id, request, cancellationToken));
    }

    /// <summary>Giden irsaliye draft güncelle.</summary>
    [HttpPut("despatches/outgoing/{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<PurchaseOrderResponse>> UpdateOutgoingDespatch(
        Guid id,
        [FromBody] SysmondUpdateOutgoingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSysmondEnabled();
        return Ok(await _despatchOrchestrator.UpdateOutgoingAsync(id, request, cancellationToken));
    }

    /// <summary>Gelen draft irsaliye sil.</summary>
    [HttpDelete("despatches/incoming/{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> DeleteIncomingDespatch(Guid id, CancellationToken cancellationToken)
    {
        EnsureSysmondEnabled();
        await _despatchOrchestrator.DeleteIncomingAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Giden draft irsaliye sil.</summary>
    [HttpDelete("despatches/outgoing/{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> DeleteOutgoingDespatch(Guid id, CancellationToken cancellationToken)
    {
        EnsureSysmondEnabled();
        await _despatchOrchestrator.DeleteOutgoingAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Siparişi onaylar (Approved).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.ApproveAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Mal kabulü yapar; stok girişi (IN) oluşur.</summary>
    [HttpPost("{id:guid}/receive")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.ReceiveAsync(id, request.ReceivedQuantities, cancellationToken);
        return NoContent();
    }

    /// <summary>Siparişi iptal eder.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.CancelAsync(id, cancellationToken);
        return NoContent();
    }

    private void EnsureSysmondEnabled()
    {
        if (!_sysmondOptions.Enabled)
            throw new InvalidOperationException("Sysmond entegrasyonu kapalı (Sysmond:Enabled=false).");
    }
}
