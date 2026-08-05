using Core.Authorization;
using Core.DTOs.Categories;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Kategori CRUD endpoint'leri.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    /// <summary>Tüm kategorileri listeler.</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _categoryService.GetAllAsync(cancellationToken));

    /// <summary>Id ile kategori getirir.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>Şirkete ait kategorileri listeler.</summary>
    [HttpGet("by-company/{companyId:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetByCompany(Guid companyId, CancellationToken cancellationToken)
        => Ok(await _categoryService.GetByCompanyIdAsync(companyId, cancellationToken));

    /// <summary>Yeni kategori oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Kategoriyi günceller.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _categoryService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Kategoriyi siler.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
