using Core.DTOs.Categories;

namespace API.Tests.Controllers.Categories;

/// <summary>Categories controller testleri için ortak yardımcılar.</summary>
internal static class CategoriesTestHelper
{
    public static CategoryResponse CreateCategoryResponse(
        Guid? id = null,
        Guid? companyId = null,
        string name = "Elektronik",
        string? description = "Elektronik ürünler") => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        Name = name,
        Description = description
    };

    public static CreateCategoryRequest CreateCreateRequest(
        string name = "Elektronik",
        string? description = "Elektronik ürünler",
        Guid? companyId = null) => new()
    {
        CompanyId = companyId ?? Guid.NewGuid(),
        Name = name,
        Description = description
    };

    public static UpdateCategoryRequest CreateUpdateRequest(
        string name = "Güncel Kategori",
        string? description = "Güncel açıklama") => new()
    {
        Name = name,
        Description = description
    };
}
