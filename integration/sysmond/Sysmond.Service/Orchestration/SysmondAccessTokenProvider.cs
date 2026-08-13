using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Services;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Orchestration;

/// <summary>
/// Sysmondax token alır ve kısa süre cache'ler (aynı request scope içinde tekrarlanmayı önler).
/// </summary>
public sealed class SysmondAccessTokenProvider : ISysmondAccessTokenProvider
{
    private readonly ISysmondTokenService _tokenService;
    private readonly ILogger<SysmondAccessTokenProvider> _logger;
    private string? _cachedToken;
    private DateTimeOffset _expiresAt;

    public SysmondAccessTokenProvider(
        ISysmondTokenService tokenService,
        ILogger<SysmondAccessTokenProvider> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTimeOffset.UtcNow < _expiresAt)
            return _cachedToken;

        var response = await _tokenService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(response.AccessToken))
            throw new InvalidOperationException("Sysmond access token alınamadı.");

        _cachedToken = response.AccessToken;
        // Expire 60s early; SysmondTokenResponse.ExpiresIn saniye cinsinden
        var lifetime = response.ExpiresIn > 0 ? response.ExpiresIn : 3600;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, lifetime - 60));

        _logger.LogDebug("Sysmond access token refreshed; expires around {ExpiresAt}", _expiresAt);
        return _cachedToken;
    }
}
