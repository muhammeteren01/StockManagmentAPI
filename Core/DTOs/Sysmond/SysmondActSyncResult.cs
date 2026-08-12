namespace Core.DTOs.Sysmond;

/// <summary>Manuel cari (act-query + act-address) senkron sonucu.</summary>
public class SysmondActSyncResult
{
    public int ActsFetched { get; set; }
    public int AddressesFetched { get; set; }

    public int ActsCreated { get; set; }
    public int ActsUpdated { get; set; }
    public int ActsDeleted { get; set; }

    public int AddressesCreated { get; set; }
    public int AddressesUpdated { get; set; }
    public int AddressesDeleted { get; set; }

    public int Failed { get; set; }
    public int FailedDeletes { get; set; }
    public int SkippedCompanyNotFound { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
