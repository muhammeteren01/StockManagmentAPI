using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>Sysmond despatch / despatch-item → StockTransaction.</summary>
public static class SysmondDespatchMapper
{
    /// <summary>DespatchDirection.Incoming</summary>
    public const int DirectionIncoming = 10;

    /// <summary>DespatchDirection.Outgoing</summary>
    public const int DirectionOutgoing = 20;

    public static TransactionType MapTransactionType(int direction) =>
        direction switch
        {
            DirectionIncoming => TransactionType.In,
            DirectionOutgoing => TransactionType.Out,
            _ => throw new InvalidOperationException($"Desteklenmeyen despatch direction: {direction}")
        };

    public static int MapQuantity(double quantity)
    {
        var qty = (int)Math.Round(quantity, MidpointRounding.AwayFromZero);
        return qty < 0 ? 0 : qty;
    }

    public static string? BuildNotes(SysmondDespatchDto header, SysmondDespatchItemDto item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(header.ActName))
            parts.Add($"Act={header.ActName.Trim()}");
        if (!string.IsNullOrWhiteSpace(header.ActVknTckn))
            parts.Add($"VKN={header.ActVknTckn.Trim()}");
        parts.Add(header.Direction == DirectionIncoming ? "Incoming" : "Outgoing");
        if (!string.IsNullOrWhiteSpace(item.Name))
            parts.Add(item.Name.Trim());
        if (!string.IsNullOrWhiteSpace(item.Description))
            parts.Add(item.Description.Trim());
        else if (!string.IsNullOrWhiteSpace(header.Description))
            parts.Add(header.Description.Trim());
        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    public static StockTransaction ToNewTransaction(
        SysmondDespatchDto header,
        SysmondDespatchItemDto item,
        Guid companyId,
        Guid productId,
        Guid warehouseId,
        Guid userId)
    {
        return new StockTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            UserId = userId,
            ExternalSysmondId = item.Id,
            ExternalSysmondDespatchId = header.Id,
            TransactionType = MapTransactionType(header.Direction),
            Quantity = MapQuantity(item.Quantity),
            ReferenceNo = Truncate(header.DocNo, 100),
            Notes = BuildNotes(header, item),
            TransactionDate = header.IssueDate == default ? DateTime.UtcNow : header.IssueDate
        };
    }

    public static void ApplyToTransaction(
        StockTransaction entity,
        SysmondDespatchDto header,
        SysmondDespatchItemDto item,
        Guid productId,
        Guid warehouseId)
    {
        entity.ExternalSysmondId = item.Id;
        entity.ExternalSysmondDespatchId = header.Id;
        entity.ProductId = productId;
        entity.WarehouseId = warehouseId;
        entity.TransactionType = MapTransactionType(header.Direction);
        entity.Quantity = MapQuantity(item.Quantity);
        entity.ReferenceNo = Truncate(header.DocNo, 100);
        entity.Notes = BuildNotes(header, item);
        entity.TransactionDate = header.IssueDate == default ? entity.TransactionDate : header.IssueDate;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
