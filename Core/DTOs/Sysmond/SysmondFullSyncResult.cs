namespace Core.DTOs.Sysmond;

/// <summary>Ürün + inventory birleşik senkron sonucu.</summary>
public class SysmondFullSyncResult
{
    public SysmondProductSyncResult Products { get; set; } = new();
    public SysmondInventorySyncResult Inventories { get; set; } = new();
}
