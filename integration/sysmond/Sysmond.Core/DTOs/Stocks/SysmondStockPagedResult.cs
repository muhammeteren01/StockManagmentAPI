namespace Integration.Sysmond.Core.DTOs.Stocks;

/// <summary>Sysmond <c>ApiResultPagedOfStockDto</c>.</summary>
public class SysmondStockPagedResult
{
    public IReadOnlyList<SysmondStockDto>? Items { get; set; }
    public long TotalCount { get; set; }
    public object? Status { get; set; }
}
