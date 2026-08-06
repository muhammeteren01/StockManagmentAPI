using Core.DTOs.StockTransactions;
using Core.Enums;

namespace API.Tests.Controllers.StockTransactions;

/// <summary>StockTransactions controller testleri için ortak yardımcılar.</summary>
internal static class StockTransactionsTestHelper
{
    public static StockTransactionResponse CreateTransactionResponse(
        Guid? id = null,
        Guid? companyId = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        Guid? userId = null,
        TransactionType transactionType = TransactionType.In,
        int quantity = 10,
        ReasonCode? reasonCode = null,
        string? referenceNo = "REF-001",
        string? notes = "Giriş") => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        ProductId = productId ?? Guid.NewGuid(),
        WarehouseId = warehouseId ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        TransactionType = transactionType,
        Quantity = quantity,
        ReasonCode = reasonCode,
        ReferenceNo = referenceNo,
        Notes = notes,
        TransactionDate = DateTime.UtcNow
    };

    public static CreateStockTransactionRequest CreateCreateRequest(
        Guid? productId = null,
        Guid? warehouseId = null,
        TransactionType transactionType = TransactionType.In,
        int quantity = 10,
        ReasonCode? reasonCode = null,
        string? referenceNo = "REF-001",
        string? notes = "Giriş") => new()
    {
        ProductId = productId ?? Guid.NewGuid(),
        WarehouseId = warehouseId ?? Guid.NewGuid(),
        TransactionType = transactionType,
        Quantity = quantity,
        ReasonCode = reasonCode,
        ReferenceNo = referenceNo,
        Notes = notes
    };
}
