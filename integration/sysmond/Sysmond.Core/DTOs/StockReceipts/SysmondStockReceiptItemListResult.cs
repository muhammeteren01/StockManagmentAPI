namespace Integration.Sysmond.Core.DTOs.StockReceipts;

/// <summary>Sysmond <c>ApiResultListOfStockReceiptItemDto</c>.</summary>
public class SysmondStockReceiptItemListResult
{
    public IReadOnlyList<SysmondStockReceiptItemDto>? Data { get; set; }
}
