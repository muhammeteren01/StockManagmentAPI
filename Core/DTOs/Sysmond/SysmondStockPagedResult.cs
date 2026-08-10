namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ApiResultPagedOfStockDto</c>.</summary>
public class SysmondStockPagedResult
{
    public IReadOnlyList<SysmondStockDto>? Items { get; set; }
    public long TotalCount { get; set; }
    public object? Status { get; set; }
}
