namespace Integration.Sysmond.Core.DTOs.Despatches;

/// <summary>
/// Yerel API: giden irsaliye oluştur (Sysmond outgoing draft → item* → save + lokal PO).
/// stockId / warehouseId = Sysmond ax id.
/// </summary>
public class SysmondCreateOutgoingDespatchRequest
{
    /// <summary>Boşsa aktif CompanyPeriod otomatik seçilir.</summary>
    public Guid? CompanyPeriodId { get; set; }

    /// <summary>Şirket adresi (Sysmond companyAddressId) — giden draft için zorunlu.</summary>
    public Guid CompanyAddressId { get; set; }

    /// <summary>Teslimat adresi — Sysmond giden draft için zorunlu.</summary>
    public SysmondDespatchDeliveryAddressCreateDto? DeliveryAddress { get; set; }

    /// <summary>Boşsa aktif dönem / PaperDespatch (50) şablon otomatik seçilir.</summary>
    public Guid? TemplateId { get; set; }

    /// <summary>DespatchScenarios: 10 Temel, 20 Hks, 30 Paper (sandbox için önerilen), 40 IDIS.
    /// 0 ise cariye göre despatch-scenarios-by-act-id ile çözülür.</summary>
    public int Scenario { get; set; } = 30;

    /// <summary>10 Sevk (varsayılan).</summary>
    public int Type { get; set; } = 10;

    public DateTime? IssueDate { get; set; }
    public DateTime? ActualDespatchDate { get; set; }
    public string? Description { get; set; }

    /// <summary>Alıcı cari (Sysmond actId). Party tipi: BuyerCustomer (20).</summary>
    public Guid ActId { get; set; }

    public string? ActName { get; set; }
    public string? ActVknTckn { get; set; }
    public string? ActTaxOfficeName { get; set; }

    public int CountryId { get; set; } = 1;
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public string? Street { get; set; }

    /// <summary>Boşsa 949 (TRY).</summary>
    public int? CurrencyId { get; set; }
    public double CurrencyExchangeRate { get; set; } = 1;

    /// <summary>Opsiyonel sipariş no etiketi (lokal OrderNumber için; Sysmond giden draft DocNo almaz).</summary>
    public string? DocNo { get; set; }

    /// <summary>
    /// Kalemlerde <c>stockPriceId</c> boşsa stock-query satış fiyatından otomatik doldurulur.
    /// </summary>
    public List<SysmondCreateIncomingDespatchItemRequest> Items { get; set; } = [];
}
