namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ApiResultListOfWarehouseStockDto</c>.</summary>
public class SysmondWarehouseStockListResult
{
    public IReadOnlyList<SysmondWarehouseStockDto>? Data { get; set; }
    public object? Status { get; set; }
}
