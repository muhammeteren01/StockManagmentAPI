using Core.DTOs.StockTransfers;
using Core.Enums;

namespace API.Tests.Controllers.StockTransfers;

/// <summary>StockTransfers controller testleri için ortak yardımcılar.</summary>
internal static class StockTransfersTestHelper
{
    public static StockTransferResponse CreateTransferResponse(
        Guid? id = null,
        Guid? companyId = null,
        Guid? fromWarehouseId = null,
        Guid? toWarehouseId = null,
        Guid? userId = null,
        StockTransferStatus status = StockTransferStatus.Pending,
        string referenceNo = "TR-001",
        string? notes = "Depolar arası",
        int itemQuantity = 5) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CompanyId = companyId ?? Guid.NewGuid(),
        FromWarehouseId = fromWarehouseId ?? Guid.NewGuid(),
        ToWarehouseId = toWarehouseId ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        Status = status,
        ReferenceNo = referenceNo,
        TransferDate = DateTime.UtcNow,
        Notes = notes,
        Items =
        [
            new StockTransferItemResponse
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = itemQuantity
            }
        ]
    };

    public static CreateStockTransferRequest CreateCreateRequest(
        Guid? fromWarehouseId = null,
        Guid? toWarehouseId = null,
        string referenceNo = "TR-001",
        string? notes = "Depolar arası",
        Guid? productId = null,
        int quantity = 5) => new()
    {
        FromWarehouseId = fromWarehouseId ?? Guid.NewGuid(),
        ToWarehouseId = toWarehouseId ?? Guid.NewGuid(),
        ReferenceNo = referenceNo,
        Notes = notes,
        Items =
        [
            new CreateStockTransferItemRequest
            {
                ProductId = productId ?? Guid.NewGuid(),
                Quantity = quantity
            }
        ]
    };
}
