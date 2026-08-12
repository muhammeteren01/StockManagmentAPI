namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>POST /api/app/incoming-despatch/draft</c> body.</summary>
public class SysmondIncomingDespatchCreateDto
{
    public Guid CompanyPeriodId { get; set; }
    public int Scenario { get; set; }
    public int Type { get; set; }
    public string? DocNo { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ActualDespatchDate { get; set; }
    public Guid? CarrierId { get; set; }
    public string? Description { get; set; }
    public int? CurrencyId { get; set; }
    public double CurrencyExchangeRate { get; set; } = 1;
    public string? IdisShipmentNo { get; set; }
    public SysmondDespatchDeliveryAddressCreateDto? DeliveryAddressCreateDto { get; set; }
    public List<SysmondDespatchPartyCreateDto>? DespatchPartyCreateDtos { get; set; }
    public Guid? OriginalDespatchId { get; set; }
}

/// <summary>Sysmond <c>DespatchPartyCreateDto</c> — draft ve POST /despatch-party ile aynı şema.</summary>
public class SysmondDespatchPartyCreateDto
{
    /// <summary>10 DeliveryCustomer, 20 BuyerCustomer, 30 SellerSupplier, 40 OriginatorCustomer.</summary>
    public int Type { get; set; } = 30;

    public Guid? ActId { get; set; }
    public string? ActVknTckn { get; set; }
    public string? ActName { get; set; }
    public string? ActTaxOfficeName { get; set; }
    public int CountryId { get; set; } = 1;
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictOther { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? PostalZone { get; set; }
    public string? Note { get; set; }
    public string? PersonFirstName { get; set; }
    public string? PersonLastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

/// <summary>Sysmond <c>POST /api/app/incoming-despatch/item</c> body.</summary>
public class SysmondDespatchItemCreateDto
{
    public Guid DespatchId { get; set; }
    public Guid? StockId { get; set; }
    public Guid? StockPriceId { get; set; }
    public string? Name { get; set; }
    public Guid? MeasureUnitId { get; set; }
    public Guid? WarehouseId { get; set; }
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double VatPercent { get; set; }
    public double DiscountPercent { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool? IsVatIncluded { get; set; }
}

/// <summary>Sysmond <c>POST /api/app/outgoing-despatch/draft</c> body.</summary>
public class SysmondOutgoingDespatchCreateDto
{
    public Guid CompanyPeriodId { get; set; }
    public int Scenario { get; set; }
    public int Type { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ActualDespatchDate { get; set; }
    public Guid? CarrierId { get; set; }
    public string? Description { get; set; }
    public int? CurrencyId { get; set; }
    public double CurrencyExchangeRate { get; set; } = 1;
    public string? ReceiverPkAlias { get; set; }
    public string? IdisShipmentNo { get; set; }
    public Guid CompanyAddressId { get; set; }
    public SysmondDespatchDeliveryAddressCreateDto? DeliveryAddressCreateDto { get; set; }
    public List<SysmondDespatchPartyCreateDto>? DespatchPartyCreateDtos { get; set; }
    public Guid? OriginalDespatchId { get; set; }
}

/// <summary>Sysmond <c>DespatchDeliveryAddressCreateDto</c>.</summary>
public class SysmondDespatchDeliveryAddressCreateDto
{
    public SysmondAddressCreateDto? Address { get; set; }
    public SysmondContactInfoCreateDto? Contact { get; set; }
}

/// <summary>Sysmond <c>AddressCreateDto</c>.</summary>
public class SysmondAddressCreateDto
{
    /// <summary>10 Business / 20 Home / 30 Other (AddressTypes).</summary>
    public int Type { get; set; } = 10;

    public int CountryId { get; set; } = 1;
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictOther { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? Room { get; set; }
    public string? Floor { get; set; }
    public string? PostalZone { get; set; }
    public string? Note { get; set; }
    public bool IsDisabled { get; set; }
}

/// <summary>Sysmond contact create (minimal).</summary>
public class SysmondContactInfoCreateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? MainPhone { get; set; }
    public string? MainCellPhone { get; set; }
}

/// <summary>Sysmond <c>POST /api/app/incoming-despatch/save</c> ve outgoing save body.</summary>
public class SysmondIncomingDespatchSaveDto
{
    public Guid CompanyId { get; set; }
    public Guid DespatchId { get; set; }
    public bool Recalculate { get; set; } = true;
}
