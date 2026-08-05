namespace Core.Entities;

/// <summary>
/// Sipariş kalemi: siparişteki ürün, adet ve birim fiyat bilgisini tutar.
/// ReceivedQuantity, kısmi mal kabullerini takip etmek için kullanılır.
/// </summary>
public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Bu miktar arttığında kod tarafında stock_transactions tablosuna IN kaydı düşmeli</summary>
    public int ReceivedQuantity { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
