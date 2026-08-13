namespace Core.DTOs.Acts;

/// <summary>Yeni cari (Act) oluşturma isteği.</summary>
public class CreateActRequest
{
    public Guid? CompanyId { get; set; }

    /// <summary>10 Customer, 20 Supplier, 30 Both, 40 Carrier.</summary>
    public int Type { get; set; } = 20;

    public string Name { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string? Title { get; set; }
    public string? ActCode { get; set; }
    public string? VknTckn { get; set; }
    public int? CountryId { get; set; } = 1;
    public int? MainCurrencyId { get; set; }
}
