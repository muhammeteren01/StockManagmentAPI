namespace Core.DTOs.Categories;

/// <summary>Kategori yanıt modeli.</summary>
public class CategoryResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
