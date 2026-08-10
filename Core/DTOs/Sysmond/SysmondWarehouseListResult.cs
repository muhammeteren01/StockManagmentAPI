namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>ApiResultListOfWarehouseDto</c> (<c>data</c> listesi).</summary>
public class SysmondWarehouseListResult
{
    public IReadOnlyList<SysmondWarehouseDto>? Data { get; set; }
    public object? Status { get; set; }
}
