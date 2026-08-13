namespace Integration.Sysmond.Core.DTOs.Sync;

/// <summary>Manuel ürün senkron sonucu (created / updated / deleted / failed / openings).</summary>
public class SysmondProductSyncResult
{
    public int Fetched { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    /// <summary>Remote set'te olmayan, ExternalSysmondId'li yerel ürün silinen sayısı.</summary>
    public int Deleted { get; set; }
    public int Failed { get; set; }
    /// <summary>Orphan silme denemesi başarısız (FK vb.).</summary>
    public int FailedDeletes { get; set; }
    public int SkippedCompanyNotFound { get; set; }
    public int OpeningsApplied { get; set; }
    public int OpeningsSkipped { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
