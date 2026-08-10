namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond <c>GET /api/app/stock-query</c> <c>StockDto</c> alanları (senkron için gerekenler).
/// Type: 10 Goods, 20 Services.
/// </summary>
public class SysmondStockDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>StockTypes: 10 = Goods, 20 = Services.</summary>
    public int Type { get; set; }

    public bool IsActive { get; set; }
    public Guid? MeasureUnitId { get; set; }
    public Guid? ActId { get; set; }
    public string? TypeName { get; set; }
    public string? MeasurementUnitName { get; set; }
    public int AmountInWarehouse { get; set; }

    public IReadOnlyList<SysmondStockPriceDto>? Prices { get; set; }

    /// <summary>
    /// Create DTO'da vardır; query yanıtında çoğu zaman yok.
    /// Varsa Inventory Quantity (adet) için kullanılır.
    /// </summary>
    public IReadOnlyList<SysmondOpeningQuantityDto>? OpeningQuantity { get; set; }
}
