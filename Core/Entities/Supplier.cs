namespace Core.Entities;

/// <summary>
/// Tedarikçi firma. Ürünlerin hangi tedarikçiden geldiğini ve
/// satın alma siparişlerinin (PurchaseOrder) kime verildiğini tutar. Şirket bazlıdır.
/// </summary>
public class Supplier
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Company Company { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
