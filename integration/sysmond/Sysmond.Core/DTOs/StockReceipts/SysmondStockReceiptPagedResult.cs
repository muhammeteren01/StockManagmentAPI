namespace Integration.Sysmond.Core.DTOs.StockReceipts;

/// <summary>Sysmond <c>ApiResultPagedOfStockReceiptDto</c>.</summary>
public class SysmondStockReceiptPagedResult
{
    public IReadOnlyList<SysmondStockReceiptDto>? Items { get; set; }
    public long TotalCount { get; set; }
}
