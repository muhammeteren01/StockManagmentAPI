namespace Integration.Sysmond.Core.DTOs.Despatches;

/// <summary>
/// Yerel API: giden irsaliye draft güncelleme.
/// Route id = Sysmond despatch id (PurchaseOrder.ExternalSysmondId).
/// </summary>
public class SysmondUpdateOutgoingDespatchRequest
{
    public Guid? CompanyPeriodId { get; set; }
    public int? Scenario { get; set; }
    public int? Type { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ActualDespatchDate { get; set; }
    public Guid? CarrierId { get; set; }
    public string? Description { get; set; }
    public int? CurrencyId { get; set; }
    public double? CurrencyExchangeRate { get; set; }
    public string? ReceiverPkAlias { get; set; }
    public string? IdisShipmentNo { get; set; }
    public SysmondDespatchDeliveryAddressUpdateDto? DeliveryAddressUpdate { get; set; }
}
