namespace Core.DTOs.Suppliers;

/// <summary>Yeni tedarikçi oluşturma isteği. CompanyId yalnızca SuperAdmin için gerekir.</summary>
public class CreateSupplierRequest
{
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
}
