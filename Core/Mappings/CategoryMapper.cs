using Core.DTOs.Categories;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Category entity ↔ DTO dönüşümleri.</summary>
public static class CategoryMapper
{
    public static Category ToEntity(CreateCategoryRequest request) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = request.CompanyId,
        Name = request.Name,
        Description = request.Description
    };

    public static void ApplyUpdate(Category entity, UpdateCategoryRequest request)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
    }

    public static CategoryResponse ToResponse(Category entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        Name = entity.Name,
        Description = entity.Description
    };
}
