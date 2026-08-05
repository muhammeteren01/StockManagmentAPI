namespace Core.Entities;

/// <summary>
/// Ürün kategorisi. Ürünleri gruplamak/filtrelemek için kullanılır. Şirket bazlıdır.
/// </summary>
public class Category
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Company Company { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
