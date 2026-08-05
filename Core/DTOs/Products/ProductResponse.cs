using Core.Enums;

namespace Core.DTOs.Products;

/// <summary>Ürün yanıt modeli.</summary>
public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid SupplierId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int MinStockLevel { get; set; }
    public ProductStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
