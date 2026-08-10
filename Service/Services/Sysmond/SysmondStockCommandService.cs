using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Core.DTOs.Sysmond;
using Core.Services;
using Core.Settings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

/// <summary>Sysmondax stock create istemcisi.</summary>
public class SysmondStockCommandService : ISysmondStockCommandService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SysmondStockCommandService> _logger;

    public SysmondStockCommandService(
        IHttpClientFactory httpClientFactory,
        ILogger<SysmondStockCommandService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateStockAsync(
        string accessToken,
        SysmondStockCreateDto body,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(body);
        if (body.CompanyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(body));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/app/stock")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond stock create başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondIdResult>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond stock create yanıtı boş veya geçersiz.");

        if (parsed.Data is null || parsed.Data.Id == Guid.Empty)
            throw new InvalidOperationException("Sysmond stock create yanıtında id yok.");

        _logger.LogInformation(
            "Sysmond stock create OK: CompanyId={CompanyId}, StockId={StockId}, Name={Name}",
            body.CompanyId,
            parsed.Data.Id,
            body.Name);

        return parsed.Data.Id;
    }

    /// <inheritdoc />
    public async Task UpdateStockAsync(
        string accessToken,
        SysmondStockUpdateDto body,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(body);
        if (body.Id == Guid.Empty)
            throw new ArgumentException("id zorunludur.", nameof(body));
        if (body.CompanyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(body));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Put, "api/app/stock")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond stock update başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        _logger.LogInformation(
            "Sysmond stock update OK: CompanyId={CompanyId}, StockId={StockId}, Name={Name}",
            body.CompanyId,
            body.Id,
            body.Name);
    }

    /// <inheritdoc />
    public async Task DeleteStockAsync(
        string accessToken,
        Guid sysmondStockId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (sysmondStockId == Guid.Empty)
            throw new ArgumentException("sysmondStockId zorunludur.", nameof(sysmondStockId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/app/stock/{sysmondStockId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond stock delete başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        _logger.LogInformation("Sysmond stock delete OK: StockId={StockId}", sysmondStockId);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
