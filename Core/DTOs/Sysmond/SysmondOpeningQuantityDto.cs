namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond açılış miktarı (OpeningQuantityCreateDto).
/// stock-query yanıtında genelde gelmez; geldiğinde depo eşlemesi best-effort yapılır.
/// </summary>
public class SysmondOpeningQuantityDto
{
    public Guid? WarehouseId { get; set; }
    public double? Quantity { get; set; }
}
