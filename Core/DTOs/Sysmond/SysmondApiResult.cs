namespace Core.DTOs.Sysmond;

/// <summary>Sysmond <c>{ status, data }</c> sarmalayıcısı.</summary>
public class SysmondApiResult<T>
{
    public T? Data { get; set; }
    public object? Status { get; set; }
    public bool? Success { get; set; }
}
