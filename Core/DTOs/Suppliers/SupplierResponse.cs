namespace Core.DTOs.Suppliers;

/// <summary>Tedarikçi yanıt modeli.</summary>
public class SupplierResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
