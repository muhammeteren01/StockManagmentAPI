using Core.DTOs.Products;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Product entity ↔ DTO dönüşümleri.</summary>
public static class ProductMapper
{
    public static Product ToEntity(CreateProductRequest request) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = request.CompanyId,
        CategoryId = request.CategoryId,
        SupplierId = request.SupplierId,
        Sku = request.Sku,
        Barcode = request.Barcode,
        Name = request.Name,
        Description = request.Description,
        UnitPrice = request.UnitPrice,
        SellingPrice = request.SellingPrice,
        MinStockLevel = request.MinStockLevel,
        Status = request.Status,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(Product entity, UpdateProductRequest request)
    {
        entity.CategoryId = request.CategoryId;
        entity.SupplierId = request.SupplierId;
        entity.Sku = request.Sku;
        entity.Barcode = request.Barcode;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UnitPrice = request.UnitPrice;
        entity.SellingPrice = request.SellingPrice;
        entity.MinStockLevel = request.MinStockLevel;
        entity.Status = request.Status;
    }

    public static ProductResponse ToResponse(Product entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        CategoryId = entity.CategoryId,
        SupplierId = entity.SupplierId,
        Sku = entity.Sku,
        Barcode = entity.Barcode,
        Name = entity.Name,
        Description = entity.Description,
        UnitPrice = entity.UnitPrice,
        SellingPrice = entity.SellingPrice,
        MinStockLevel = entity.MinStockLevel,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
