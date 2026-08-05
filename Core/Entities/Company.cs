namespace Core.Entities;

/// <summary>
/// Sisteme kayıtlı şirket/firma (tenant). Multi-company yapının kök tablosudur;
/// kullanıcılar, depolar, ürünler ve siparişler şirket bazında izole edilir.
/// </summary>
public class Company
{
    public Guid Id { get; set; }

    /// <summary>Şirket/Firma Adı</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Vergi Dairesi</summary>
    public string? TaxOffice { get; set; }

    /// <summary>Vergi No</summary>
    public string? TaxNumber { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<StockTransfer> StockTransfers { get; set; } = new List<StockTransfer>();
}
