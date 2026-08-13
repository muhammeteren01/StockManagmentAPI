namespace Integration.Sysmond.Core.DTOs.StockReceipts;

/// <summary>Sysmond <c>StockReceiptTypes</c>: UI stocks/entry|exit|transfer.</summary>
public static class SysmondStockReceiptTypes
{
    public const int Entry = 10;
    public const int Exit = 20;
    public const int Adjustment = 30;
    public const int Loss = 40;
    public const int Transfer = 50;
}
