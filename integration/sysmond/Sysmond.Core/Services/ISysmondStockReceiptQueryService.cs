using Integration.Sysmond.Core.DTOs.StockReceipts;

namespace Integration.Sysmond.Core.Services;

/// <summary>Sysmondax stock-receipt (stocks/entry|exit|transfer) HTTP istemcisi.</summary>
public interface ISysmondStockReceiptQueryService
{
    /// <summary>
    /// <c>GET /api/app/stock-receipt/stock-receipt-list</c> tüm sayfalar.
    /// <paramref name="type"/>: 10 Entry, 20 Exit, 50 Transfer (null = hepsi).
    /// </summary>
    Task<IReadOnlyList<SysmondStockReceiptDto>> GetStockReceiptsAsync(
        string accessToken,
        Guid companyPeriodId,
        int? type = null,
        bool? isDraft = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /api/app/stock-receipt/{id}/stock-receipt-item-list</c>.
    /// </summary>
    Task<IReadOnlyList<SysmondStockReceiptItemDto>> GetStockReceiptItemsAsync(
        string accessToken,
        Guid stockReceiptId,
        CancellationToken cancellationToken = default);
}
