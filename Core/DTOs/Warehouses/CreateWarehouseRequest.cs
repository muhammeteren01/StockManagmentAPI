namespace Core.DTOs.Warehouses;

/// <summary>Yeni depo oluşturma isteği. CompanyId yalnızca SuperAdmin için gerekir.</summary>
public class CreateWarehouseRequest
{
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? Capacity { get; set; }
}
