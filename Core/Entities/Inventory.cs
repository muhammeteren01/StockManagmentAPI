namespace Core.Entities;

/// <summary>
/// Depo bazında güncel stok durumu (ürün + depo başına tek satır).
/// Her stok hareketi (StockTransaction) sonrası Quantity güncellenir.
/// </summary>
public class Inventory
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Sysmond WarehouseStock.Id (varsa). Yoksa orphan silme Product+Warehouse ExternalSysmondId ile yapılır.
    /// </summary>
    public Guid? ExternalSysmondId { get; set; }

    /// <summary>Gerçek zamanlı stok miktarı</summary>
    public int Quantity { get; set; }

    public DateTime LastUpdated { get; set; }

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = null!;

    public Company Company { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}
