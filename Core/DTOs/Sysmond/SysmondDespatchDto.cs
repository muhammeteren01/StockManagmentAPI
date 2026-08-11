namespace Core.DTOs.Sysmond;

/// <summary>
/// Sysmond <c>GET /api/app/despatch-query/despatches</c> <c>DespatchDto</c> (senkron için gereken alanlar).
/// Direction: 10 Incoming, 20 Outgoing.
/// Status: 10 Draft … -100 Cancelled (bkz. swagger DespatchStatuses).
/// </summary>
public class SysmondDespatchDto
{
    public Guid Id { get; set; }
    public Guid? Ettn { get; set; }
    public int Direction { get; set; }
    public int Status { get; set; }
    public string? DocNo { get; set; }
    public Guid CompanyPeriodId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ActualDespatchDate { get; set; }
    public string? Description { get; set; }
    public string? ActName { get; set; }
    public string? ActVknTckn { get; set; }

    /// <summary>Şirket adresi (Sysmond companyAddressId).</summary>
    public Guid? CompanyAddressId { get; set; }

    /// <summary>Teslim adresi kaydı (Sysmond deliveryAddressId); doluysa detail endpoint çağrılır.</summary>
    public Guid? DeliveryAddressId { get; set; }
}
