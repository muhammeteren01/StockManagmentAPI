namespace Core.DTOs.Sysmond;

/// <summary>Manuel irsaliye (despatch) → PurchaseOrder belge senkron sonucu.</summary>
public class SysmondDespatchSyncResult
{
    /// <summary>Remote despatch header sayısı.</summary>
    public int DespatchesFetched { get; set; }

    /// <summary>Kalem (despatch-item) satır sayısı.</summary>
    public int ItemsFetched { get; set; }

    public int Created { get; set; }
    public int Updated { get; set; }

    /// <summary>Remote set'te olmayan ExternalSysmondId'li yerel belge silinen sayısı.</summary>
    public int Deleted { get; set; }

    public int Failed { get; set; }
    public int FailedDeletes { get; set; }

    public int SkippedCompanyNotFound { get; set; }
    public int SkippedProductNotFound { get; set; }
    public int SkippedWarehouseNotFound { get; set; }
    public int SkippedInvalidItem { get; set; }
    public int SkippedNoUser { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
