using Core.DTOs.Products;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>Product entity ↔ DTO dönüşümleri.</summary>
public static class ProductMapper
{
    public static Product ToEntity(CreateProductRequest request, Guid companyId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        CategoryId = NormalizeOptionalGuid(request.CategoryId),
        SupplierId = NormalizeOptionalGuid(request.SupplierId),
        Type = request.Type,
        MeasureUnitId = request.MeasureUnitId,
        ExternalSysmondId = request.ExternalSysmondId,
        Sku = request.Sku,
        Barcode = request.Barcode,
        Name = request.Name,
        Description = request.Description,
        UnitPrice = request.UnitPrice,
        PurchaseCurrencyId = request.PurchaseCurrencyId,
        SellingPrice = request.SellingPrice,
        SaleCurrencyId = request.SaleCurrencyId,
        MinStockLevel = request.MinStockLevel,
        Status = request.Status,
        CreatedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(Product entity, UpdateProductRequest request)
    {
        entity.CategoryId = NormalizeOptionalGuid(request.CategoryId);
        entity.SupplierId = NormalizeOptionalGuid(request.SupplierId);
        entity.Type = request.Type;
        entity.MeasureUnitId = request.MeasureUnitId;
        entity.ExternalSysmondId = request.ExternalSysmondId;
        entity.Sku = request.Sku;
        entity.Barcode = request.Barcode;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UnitPrice = request.UnitPrice;
        entity.PurchaseCurrencyId = request.PurchaseCurrencyId;
        entity.SellingPrice = request.SellingPrice;
        entity.SaleCurrencyId = request.SaleCurrencyId;
        entity.MinStockLevel = request.MinStockLevel;
        entity.Status = request.Status;
    }

    public static ProductResponse ToResponse(Product entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        CategoryId = entity.CategoryId,
        SupplierId = entity.SupplierId,
        Type = entity.Type,
        MeasureUnitId = entity.MeasureUnitId,
        ExternalSysmondId = entity.ExternalSysmondId,
        Sku = entity.Sku,
        Barcode = entity.Barcode,
        Name = entity.Name,
        Description = entity.Description,
        UnitPrice = entity.UnitPrice,
        PurchaseCurrencyId = entity.PurchaseCurrencyId,
        SellingPrice = entity.SellingPrice,
        SaleCurrencyId = entity.SaleCurrencyId,
        MinStockLevel = entity.MinStockLevel,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };

    private static Guid? NormalizeOptionalGuid(Guid? value)
        => value is null || value == Guid.Empty ? null : value;
}
