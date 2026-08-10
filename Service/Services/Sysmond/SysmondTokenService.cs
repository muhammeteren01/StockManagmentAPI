using System.Net.Http.Headers;
using System.Text.Json;
using Core.DTOs.Sysmond;
using Core.Exceptions;
using Core.Services;
using Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Service.Services.Sysmond;

/// <summary>Sysmondax <c>/connect/token</c> ile OAuth password grant access token alır.</summary>
public class SysmondTokenService : ISysmondTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SysmondOptions _options;
    private readonly ILogger<SysmondTokenService> _logger;

    public SysmondTokenService(
        IHttpClientFactory httpClientFactory,
        IOptions<SysmondOptions> options,
        ILogger<SysmondTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SysmondTokenResponse> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var username = _options.Username.Trim();
        var password = _options.Password.Trim();
        var clientId = _options.ClientId.Trim();
        var clientSecret = _options.ClientSecret.Trim();
        var scope = string.IsNullOrWhiteSpace(_options.Scope)
            ? SysmondOptions.DefaultScope
            : _options.Scope.Trim();

            var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var clientIdPrefix = clientId.Length <= 12
                ? clientId
                : clientId[..12] + "...";

            _logger.LogDebug(
                "Sysmond token request: BaseUrl={BaseUrl}, grant_type=password, usernameLength={UsernameLength}, client_id={ClientIdPrefix}, scope={Scope}",
                client.BaseAddress?.ToString()?.TrimEnd('/'),
                username.Length,
                clientIdPrefix,
                scope);
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = scope
        });
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var response = await client.PostAsync("connect/token", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = string.IsNullOrWhiteSpace(body)
                ? $"Sysmond token isteği başarısız ({(int)response.StatusCode})."
                : $"Sysmond token isteği başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}";

            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                    => new UnauthorizedException(detail),
                _ => new InvalidOperationException(detail)
            };
        }

        var token = JsonSerializer.Deserialize<SysmondTokenResponse>(body, JsonOptions);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("Sysmond token yanıtı geçersiz veya boş.");

        return token;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("Sysmond:BaseUrl yapılandırılmamış.");

        if (string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Sysmond kimlik bilgileri eksik. User Secrets ile Sysmond:Username, Sysmond:Password, Sysmond:ClientId ve Sysmond:ClientSecret ayarlayın.");
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
