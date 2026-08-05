namespace Core.DTOs.Inventories;

/// <summary>Stok satırı yanıt modeli.</summary>
public class InventoryResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; }
}
