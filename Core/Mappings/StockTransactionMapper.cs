using Core.DTOs.StockTransactions;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>StockTransaction entity ↔ DTO dönüşümleri.</summary>
public static class StockTransactionMapper
{
    public static string ToTypeLabel(TransactionType type) => type switch
    {
        TransactionType.In => "Giriş",
        TransactionType.Out => "Çıkış",
        TransactionType.TransferOut => "Transfer Çıkış",
        TransactionType.TransferIn => "Transfer Giriş",
        TransactionType.Adjustment => "Düzeltme",
        _ => type.ToString()
    };

    public static StockTransaction ToEntity(CreateStockTransactionRequest request, Guid companyId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        ProductId = request.ProductId,
        WarehouseId = request.WarehouseId,
        UserId = userId,
        TransferId = request.TransferId,
        PurchaseOrderId = request.PurchaseOrderId,
        TransactionType = request.TransactionType,
        Quantity = request.Quantity,
        ReasonCode = request.ReasonCode,
        ReferenceNo = request.ReferenceNo,
        Notes = request.Notes,
        TransactionDate = DateTime.UtcNow
    };

    public static StockTransactionResponse ToResponse(StockTransaction entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        ProductId = entity.ProductId,
        WarehouseId = entity.WarehouseId,
        UserId = entity.UserId,
        TransferId = entity.TransferId,
        PurchaseOrderId = entity.PurchaseOrderId,
        TransactionType = entity.TransactionType,
        TransactionTypeLabel = ToTypeLabel(entity.TransactionType),
        Quantity = entity.Quantity,
        ReasonCode = entity.ReasonCode,
        ReferenceNo = entity.ReferenceNo,
        Notes = entity.Notes,
        TransactionDate = entity.TransactionDate,
        ExternalSysmondId = entity.ExternalSysmondId,
        ExternalSysmondReceiptId = entity.ExternalSysmondDespatchId
    };
}
