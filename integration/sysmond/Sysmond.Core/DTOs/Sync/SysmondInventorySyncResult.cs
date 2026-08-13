namespace Integration.Sysmond.Core.DTOs.Sync;

/// <summary>Manuel inventory (depo stok) senkron sonucu.</summary>
public class SysmondInventorySyncResult
{
    /// <summary>Remote stock-balance satır sayısı.</summary>
    public int Fetched { get; set; }

    public int WarehousesFetched { get; set; }
    public int WarehousesCreated { get; set; }
    public int WarehousesUpdated { get; set; }

    public int Created { get; set; }
    public int Updated { get; set; }

    /// <summary>Remote set'te olmayan Sysmond-linked yerel inventory silinen sayısı.</summary>
    public int Deleted { get; set; }

    public int Failed { get; set; }
    public int FailedDeletes { get; set; }

    public int SkippedCompanyNotFound { get; set; }
    public int SkippedProductNotFound { get; set; }
    public int SkippedWarehouseNotFound { get; set; }

    /// <summary>warehouse-stock 403 vb. yumuşak uyarılar dahil.</summary>
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
