using Core.DTOs.StockTransfers;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>StockTransfer entity ↔ DTO dönüşümleri.</summary>
public static class StockTransferMapper
{
    public static StockTransfer ToEntity(CreateStockTransferRequest request, Guid companyId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        FromWarehouseId = request.FromWarehouseId,
        ToWarehouseId = request.ToWarehouseId,
        UserId = userId,
        ReferenceNo = request.ReferenceNo,
        Notes = request.Notes,
        Status = StockTransferStatus.Pending,
        TransferDate = DateTime.UtcNow,
        Items = request.Items.Select(i => new StockTransferItem
        {
            Id = Guid.NewGuid(),
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList()
    };

    public static StockTransferResponse ToResponse(StockTransfer entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
        FromWarehouseId = entity.FromWarehouseId,
        ToWarehouseId = entity.ToWarehouseId,
        UserId = entity.UserId,
        Status = entity.Status,
        ReferenceNo = entity.ReferenceNo,
        TransferDate = entity.TransferDate,
        CompletionDate = entity.CompletionDate,
        Notes = entity.Notes,
        Items = entity.Items?.Select(i => new StockTransferItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList() ?? []
    };
}
