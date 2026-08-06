using Core.DTOs.Products;
using Core.Enums;

namespace API.Tests.Controllers.Products;

/// <summary>Products controller testleri için ortak yardımcılar.</summary>
internal static class ProductsTestHelper
{
    public static ProductResponse CreateProductResponse(
        Guid? id = null,
        Guid? companyId = null,
        Guid? categoryId = null,
        Guid? supplierId = null,
        string sku = "SKU-001",
        string? barcode = "8690000000001",
        string name = "Laptop",
        string description = "15 inç laptop",
        decimal unitPrice = 10000m,
        decimal sellingPrice = 12500m,
        int minStockLevel = 5,
        ProductStatus status = ProductStatus.Active) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        CategoryId = categoryId ?? Guid.NewGuid(),
        SupplierId = supplierId ?? Guid.NewGuid(),
        Sku = sku,
        Barcode = barcode,
        Name = name,
        Description = description,
        UnitPrice = unitPrice,
        SellingPrice = sellingPrice,
        MinStockLevel = minStockLevel,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    public static CreateProductRequest CreateCreateRequest(
        Guid? companyId = null,
        Guid? categoryId = null,
        Guid? supplierId = null,
        string sku = "SKU-001",
        string? barcode = "8690000000001",
        string name = "Laptop",
        string description = "15 inç laptop",
        decimal unitPrice = 10000m,
        decimal sellingPrice = 12500m,
        int minStockLevel = 5,
        ProductStatus status = ProductStatus.Active) => new()
    {
        CompanyId = companyId ?? Guid.NewGuid(),
        CategoryId = categoryId ?? Guid.NewGuid(),
        SupplierId = supplierId ?? Guid.NewGuid(),
        Sku = sku,
        Barcode = barcode,
        Name = name,
        Description = description,
        UnitPrice = unitPrice,
        SellingPrice = sellingPrice,
        MinStockLevel = minStockLevel,
        Status = status
    };

    public static UpdateProductRequest CreateUpdateRequest(
        Guid? categoryId = null,
        Guid? supplierId = null,
        string sku = "SKU-002",
        string? barcode = "8690000000002",
        string name = "Güncel Laptop",
        string description = "Güncel açıklama",
        decimal unitPrice = 11000m,
        decimal sellingPrice = 13500m,
        int minStockLevel = 8,
        ProductStatus status = ProductStatus.Active) => new()
    {
        CategoryId = categoryId ?? Guid.NewGuid(),
        SupplierId = supplierId ?? Guid.NewGuid(),
        Sku = sku,
        Barcode = barcode,
        Name = name,
        Description = description,
        UnitPrice = unitPrice,
        SellingPrice = sellingPrice,
        MinStockLevel = minStockLevel,
        Status = status
    };
}
