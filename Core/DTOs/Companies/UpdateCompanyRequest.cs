namespace Core.DTOs.Companies;

/// <summary>Şirket güncelleme isteği.</summary>
public class UpdateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
