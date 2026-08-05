namespace Core.DTOs.Warehouses;

/// <summary>Depo güncelleme isteği.</summary>
public class UpdateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
