namespace Integration.Sysmond.Core.DTOs.Sync;

/// <summary>Ürün + inventory birleşik senkron sonucu.</summary>
public class SysmondFullSyncResult
{
    public SysmondProductSyncResult Products { get; set; } = new();
    public SysmondInventorySyncResult Inventories { get; set; } = new();
}
