using Core.Entities;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Ürün CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Tüm ürünleri listeler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetAllAsync(cancellationToken));
    }

    /// <summary>Id ile ürün getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Şirkete ait ürünleri listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetByCompanyIdAsync(companyId, cancellationToken));
    }

    /// <summary>Yeni ürün oluşturur.</summary>
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product, CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(product, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Ürünü günceller.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Product product, CancellationToken cancellationToken)
    {
        if (id != product.Id)
            return BadRequest("Id uyuşmuyor.");

        await _productService.UpdateAsync(product, cancellationToken);
        return NoContent();
    }

    /// <summary>Ürünü siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
