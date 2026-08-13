using Core.Entities;
using Core.Enums;
using Integration.Sysmond.Core.DTOs.StockReceipts;

namespace Integration.Sysmond.Core.Mappings;

/// <summary>Sysmond stock-receipt → StockTransaction (giriş/çıkış/transfer).</summary>
public static class SysmondStockReceiptMapper
{
    /// <summary>
    /// TransferIn satırı için deterministic ExternalSysmondId
    /// (TransferOut item.Id kullanır; unique index çakışmasın).
    /// </summary>
    public static Guid ToTransferInExternalId(Guid receiptItemId)
    {
        var bytes = receiptItemId.ToByteArray();
        bytes[0] ^= 0x5A;
        bytes[1] ^= 0xA5;
        return new Guid(bytes);
    }

    public static int MapQuantity(double quantity)
    {
        var qty = (int)Math.Round(quantity, MidpointRounding.AwayFromZero);
        return qty < 0 ? 0 : qty;
    }

    public static string TypeLabel(int receiptType) => receiptType switch
    {
        SysmondStockReceiptTypes.Entry => "Giriş",
        SysmondStockReceiptTypes.Exit => "Çıkış",
        SysmondStockReceiptTypes.Transfer => "Transfer",
        SysmondStockReceiptTypes.Adjustment => "Düzeltme",
        SysmondStockReceiptTypes.Loss => "Fire",
        _ => $"Type={receiptType}"
    };

    public static TransactionType MapTransactionType(int receiptType, bool isTransferIn = false) =>
        receiptType switch
        {
            SysmondStockReceiptTypes.Entry => TransactionType.In,
            SysmondStockReceiptTypes.Exit => TransactionType.Out,
            SysmondStockReceiptTypes.Transfer => isTransferIn ? TransactionType.TransferIn : TransactionType.TransferOut,
            SysmondStockReceiptTypes.Adjustment => TransactionType.Adjustment,
            SysmondStockReceiptTypes.Loss => TransactionType.Adjustment,
            _ => throw new InvalidOperationException($"Desteklenmeyen stock-receipt type: {receiptType}")
        };

    public static ReasonCode? MapReasonCode(int receiptType) =>
        receiptType == SysmondStockReceiptTypes.Loss ? ReasonCode.Lost : null;

    public static string? BuildNotes(SysmondStockReceiptDto receipt, SysmondStockReceiptItemDto item, bool isTransferIn = false)
    {
        var parts = new List<string> { $"Sysmond {TypeLabel(receipt.Type)}" };
        if (receipt.Type == SysmondStockReceiptTypes.Transfer)
            parts.Add(isTransferIn ? "TransferIn" : "TransferOut");
        if (!string.IsNullOrWhiteSpace(receipt.TypeName))
            parts.Add(receipt.TypeName.Trim());
        if (!string.IsNullOrWhiteSpace(item.StockName))
            parts.Add(item.StockName.Trim());
        if (!string.IsNullOrWhiteSpace(receipt.Description))
            parts.Add(receipt.Description.Trim());
        return string.Join(" | ", parts);
    }

    public static StockTransaction ToNewTransaction(
        SysmondStockReceiptDto receipt,
        SysmondStockReceiptItemDto item,
        Guid companyId,
        Guid productId,
        Guid warehouseId,
        Guid userId,
        Guid externalSysmondId,
        TransactionType transactionType,
        bool isTransferIn = false)
    {
        return new StockTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            UserId = userId,
            ExternalSysmondId = externalSysmondId,
            // Header gruplama: stock-receipt.id (despatch kolonunu belge id olarak yeniden kullanır)
            ExternalSysmondDespatchId = receipt.Id,
            ExternalSysmondCompanyPeriodId = NormalizePeriodId(receipt.CompanyPeriodId),
            TransactionType = transactionType,
            Quantity = MapQuantity(item.Quantity),
            ReasonCode = MapReasonCode(receipt.Type),
            ReferenceNo = Truncate(receipt.DocNo, 100),
            Notes = BuildNotes(receipt, item, isTransferIn),
            TransactionDate = receipt.TransactionDate ?? DateTime.UtcNow
        };
    }

    public static void ApplyToTransaction(
        StockTransaction entity,
        SysmondStockReceiptDto receipt,
        SysmondStockReceiptItemDto item,
        Guid productId,
        Guid warehouseId,
        Guid externalSysmondId,
        TransactionType transactionType,
        bool isTransferIn = false)
    {
        entity.ExternalSysmondId = externalSysmondId;
        entity.ExternalSysmondDespatchId = receipt.Id;
        entity.ExternalSysmondCompanyPeriodId = NormalizePeriodId(receipt.CompanyPeriodId);
        entity.ProductId = productId;
        entity.WarehouseId = warehouseId;
        entity.TransactionType = transactionType;
        entity.Quantity = MapQuantity(item.Quantity);
        entity.ReasonCode = MapReasonCode(receipt.Type);
        entity.ReferenceNo = Truncate(receipt.DocNo, 100);
        entity.Notes = BuildNotes(receipt, item, isTransferIn);
        if (receipt.TransactionDate is DateTime dt && dt != default)
            entity.TransactionDate = dt;
    }

    private static Guid? NormalizePeriodId(Guid companyPeriodId)
        => companyPeriodId == Guid.Empty ? null : companyPeriodId;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
