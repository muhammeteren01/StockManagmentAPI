namespace Integration.Sysmond.Core.DTOs.Common;

/// <summary>Sysmond <c>ApiResultOfIdModel</c>.</summary>
public class SysmondIdResult
{
    public SysmondIdModel? Data { get; set; }
    public object? Status { get; set; }
}

public class SysmondIdModel
{
    public Guid Id { get; set; }
}
