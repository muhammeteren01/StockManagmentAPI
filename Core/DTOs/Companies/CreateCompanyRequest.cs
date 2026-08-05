namespace Core.DTOs.Companies;

/// <summary>Yeni şirket oluşturma isteği.</summary>
public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
