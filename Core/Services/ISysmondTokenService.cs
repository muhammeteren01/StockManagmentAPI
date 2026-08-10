using Core.DTOs.Sysmond;

namespace Core.Services;

/// <summary>Sysmondax OAuth password grant token alma.</summary>
public interface ISysmondTokenService
{
    /// <summary>
    /// Yapılandırmadaki partner/client secret ile <c>POST /connect/token</c> çağırır.
    /// </summary>
    Task<SysmondTokenResponse> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
