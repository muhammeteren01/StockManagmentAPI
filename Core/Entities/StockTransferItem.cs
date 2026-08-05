namespace Core.Entities;

/// <summary>
/// Transfer kalemi: bir transferde hangi üründen kaç adet taşındığını tutar.
/// </summary>
public class StockTransferItem
{
    public Guid Id { get; set; }
    public Guid TransferId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }

    public StockTransfer Transfer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
