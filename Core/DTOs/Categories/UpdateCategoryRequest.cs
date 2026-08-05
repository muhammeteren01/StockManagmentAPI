namespace Core.DTOs.Categories;

/// <summary>Kategori güncelleme isteği.</summary>
public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
