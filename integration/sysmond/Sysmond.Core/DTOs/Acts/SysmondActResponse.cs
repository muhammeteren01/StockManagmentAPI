namespace Integration.Sysmond.Core.DTOs.Acts;

/// <summary>Sysmond cari güncelleme yanıtı (yerel Act özeti).</summary>
public class SysmondActResponse
{
    public Guid Id { get; set; }
    public Guid ExternalSysmondId { get; set; }
    public int Type { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Title { get; set; }
    public string? ActCode { get; set; }
    public string? VknTckn { get; set; }
    public string? TaxOfficeName { get; set; }
    public string? ActFullAddress { get; set; }
    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public int Scenario { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsAbroadCustomer { get; set; }
    public DateTime SyncedAt { get; set; }
}
