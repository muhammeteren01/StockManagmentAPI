namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond <c>GET /api/app/despatch-query/{id}/despatch-items</c> <c>DespatchItemDto</c>.
/// </summary>
public class SysmondDespatchItemDto
{
    public Guid Id { get; set; }
    public Guid DespatchId { get; set; }
    public Guid? StockId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public double Quantity { get; set; }
    public string? Description { get; set; }
}
