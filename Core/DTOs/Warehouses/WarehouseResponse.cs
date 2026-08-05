namespace Core.DTOs.Warehouses;

/// <summary>Depo yanıt modeli.</summary>
public class WarehouseResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
}
