namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ApiResultPagedOfDespatchDto</c>.</summary>
public class SysmondDespatchPagedResult
{
    public IReadOnlyList<SysmondDespatchDto>? Items { get; set; }
    public long TotalCount { get; set; }
    public object? Status { get; set; }
}
