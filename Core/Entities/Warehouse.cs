namespace Core.Entities;

/// <summary>
/// Depo. Stoklar (Inventory) depo bazında tutulur; transferler depolar arasında yapılır.
/// OutgoingTransfers = FromWarehouseId, IncomingTransfers = ToWarehouseId ile eşleşir.
/// </summary>
public class Warehouse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>Sysmond Warehouse.Id; null = yerel oluşturulan depo.</summary>
    public Guid? ExternalSysmondId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<StockTransfer> OutgoingTransfers { get; set; } = new List<StockTransfer>();
    public ICollection<StockTransfer> IncomingTransfers { get; set; } = new List<StockTransfer>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
