using Core.DTOs.StockTransactions;
using Core.Entities;

namespace Core.Mappings;

/// <summary>StockTransaction entity ↔ DTO dönüşümleri.</summary>
public static class StockTransactionMapper
{
    public static StockTransaction ToEntity(CreateStockTransactionRequest request) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = request.ProductId,
        WarehouseId = request.WarehouseId,
        UserId = request.UserId,
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
        ProductId = entity.ProductId,
        WarehouseId = entity.WarehouseId,
        UserId = entity.UserId,
        TransferId = entity.TransferId,
        PurchaseOrderId = entity.PurchaseOrderId,
        TransactionType = entity.TransactionType,
        Quantity = entity.Quantity,
        ReasonCode = entity.ReasonCode,
        ReferenceNo = entity.ReferenceNo,
        Notes = entity.Notes,
        TransactionDate = entity.TransactionDate
    };
}
