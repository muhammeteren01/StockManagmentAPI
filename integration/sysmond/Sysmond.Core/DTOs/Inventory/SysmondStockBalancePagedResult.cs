namespace Integration.Sysmond.Core.DTOs.Inventory;

/// <summary>Sysmond <c>ApiResultPagedOfStockBalanceDto</c>.</summary>
public class SysmondStockBalancePagedResult
{
    public IReadOnlyList<SysmondStockBalanceDto>? Items { get; set; }
    public long TotalCount { get; set; }
    public object? Status { get; set; }
}
