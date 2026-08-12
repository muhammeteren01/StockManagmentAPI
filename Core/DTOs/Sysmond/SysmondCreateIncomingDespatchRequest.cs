namespace Core.DTOs.Sysmond;

/// <summary>
/// Yerel API: gelen irsaliye oluştur (Sysmond draft → item* → save + lokal PO).
/// stockId / warehouseId = Sysmond ax id (Product/Warehouse.ExternalSysmondId).
/// </summary>
public class SysmondCreateIncomingDespatchRequest
{
    /// <summary>Boşsa aktif CompanyPeriod otomatik seçilir.</summary>
    public Guid? CompanyPeriodId { get; set; }

    /// <summary>10 Temel (varsayılan), 20 Hks, 30 Paper, 40 IDIS.</summary>
    public int Scenario { get; set; } = 10;

    /// <summary>10 Sevk (varsayılan), 20 Matbu.</summary>
    public int Type { get; set; } = 10;

    public string? DocNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ActualDespatchDate { get; set; }
    public string? Description { get; set; }

    /// <summary>Karşı cari (Sysmond actId). Party tipi: SellerSupplier.</summary>
    public Guid ActId { get; set; }

    public string? ActName { get; set; }
    public string? ActVknTckn { get; set; }
    public string? ActTaxOfficeName { get; set; }

    /// <summary>Ülke (Sysmond countryId); varsayılan 1.</summary>
    public int CountryId { get; set; } = 1;

    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public string? Street { get; set; }

    public int? CurrencyId { get; set; }
    public double CurrencyExchangeRate { get; set; } = 1;

    public List<SysmondCreateIncomingDespatchItemRequest> Items { get; set; } = [];
}

public class SysmondCreateIncomingDespatchItemRequest
{
    public Guid StockId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid MeasureUnitId { get; set; }

    /// <summary>
    /// Sysmond stok fiyat Id. Giden create'te boş bırakılırsa satış fiyatı otomatik seçilir.
    /// </summary>
    public Guid? StockPriceId { get; set; }
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double VatPercent { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
}
