namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ApiResultListOfDespatchItemDto</c> (<c>data</c> listesi).</summary>
public class SysmondDespatchItemListResult
{
    public IReadOnlyList<SysmondDespatchItemDto>? Data { get; set; }
    public object? Status { get; set; }
}
