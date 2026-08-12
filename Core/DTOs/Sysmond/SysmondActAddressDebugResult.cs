namespace Core.DTOs.Sysmond;

/// <summary>act-address ham yanıt + parse sonucu (tanılama).</summary>
public class SysmondActAddressDebugResult
{
    public int HttpStatusCode { get; set; }
    public int ParsedCount { get; set; }
    public IReadOnlyList<SysmondActAddressDto> Items { get; set; } = Array.Empty<SysmondActAddressDto>();
    public string RawBody { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public IReadOnlyList<string> AttemptedUrls { get; set; } = Array.Empty<string>();
    public bool? CanAccessAddressAndContactInfo { get; set; }
}
