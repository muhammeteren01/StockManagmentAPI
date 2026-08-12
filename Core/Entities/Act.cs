namespace Core.Entities;

/// <summary>
/// Sysmond cari hesabı (müşteri / tedarikçi / her ikisi / taşıyıcı).
/// <see cref="ExternalSysmondId"/> = Sysmond Act.Id.
/// </summary>
public class Act
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>Sysmond Act.Id.</summary>
    public Guid ExternalSysmondId { get; set; }

    /// <summary>ActTypes: 10 Customer, 20 Supplier, 30 CustomerAndSupplier, 40 Carrier.</summary>
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

    /// <summary>InvoiceScenarios (irsaliye/fatura senaryosu).</summary>
    public int Scenario { get; set; }

    public bool IsDisabled { get; set; }
    public bool IsLocked { get; set; }
    public bool IsAbroadCustomer { get; set; }

    /// <summary>Sysmond ParentActId (varsa).</summary>
    public Guid? ParentActExternalId { get; set; }

    public DateTime SyncedAt { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<ActAddress> Addresses { get; set; } = new List<ActAddress>();
}
