namespace Core.Entities;

/// <summary>
/// Depo bazında güncel stok durumu (ürün + depo başına tek satır).
/// Her stok hareketi (StockTransaction) sonrası Quantity güncellenir.
/// </summary>
public class Inventory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }

    /// <summary>Gerçek zamanlı stok miktarı</summary>
    public int Quantity { get; set; }

    public DateTime LastUpdated { get; set; }

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
