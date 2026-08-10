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

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
