using Core.Enums;

namespace Core.DTOs.PurchaseOrders;

/// <summary>Sipariş / irsaliye kalemi yanıt modeli.</summary>
public class PurchaseOrderItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ExternalSysmondId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? VatPercent { get; set; }
    public int ReceivedQuantity { get; set; }
}

/// <summary>Satın alma siparişi / irsaliye belge yanıt modeli.</summary>
public class PurchaseOrderResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public PurchaseOrderDocumentType DocumentType { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public DespatchDirection? Direction { get; set; }
    public Guid? ExternalSysmondId { get; set; }
    public Guid? ExternalSysmondCompanyPeriodId { get; set; }
    public string? DeliveryAddressJson { get; set; }
    public string? ActName { get; set; }
    public string? ActVknTckn { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ActualDespatchDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderItemResponse> Items { get; set; } = new();
}
