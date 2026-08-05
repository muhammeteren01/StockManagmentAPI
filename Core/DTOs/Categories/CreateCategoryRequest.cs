namespace Core.DTOs.Categories;

/// <summary>Yeni kategori oluşturma isteği.</summary>
public class CreateCategoryRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
