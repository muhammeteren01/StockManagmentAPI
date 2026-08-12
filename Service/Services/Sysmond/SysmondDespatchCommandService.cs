using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.DTOs.Sysmond;
using Core.Services;
using Core.Settings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

/// <summary>Sysmondax incoming/outgoing despatch create istemcisi.</summary>
public class SysmondDespatchCommandService : ISysmondDespatchCommandService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SysmondDespatchCommandService> _logger;

    public SysmondDespatchCommandService(
        IHttpClientFactory httpClientFactory,
        ILogger<SysmondDespatchCommandService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Guid> CreateIncomingDraftAsync(
        string accessToken,
        SysmondIncomingDespatchCreateDto body,
        CancellationToken cancellationToken = default)
        => PostForIdAsync(accessToken, "api/app/incoming-despatch/draft", body, "incoming-despatch/draft", cancellationToken);

    /// <inheritdoc />
    public Task<Guid> CreateIncomingItemAsync(
        string accessToken,
        SysmondDespatchItemCreateDto body,
        CancellationToken cancellationToken = default)
        => PostForIdAsync(accessToken, "api/app/incoming-despatch/item", body, "incoming-despatch/item", cancellationToken);

    /// <inheritdoc />
    public Task SaveIncomingAsync(
        string accessToken,
        SysmondIncomingDespatchSaveDto body,
        CancellationToken cancellationToken = default)
        => PostSaveAsync(accessToken, "api/app/incoming-despatch/save", body, "incoming-despatch/save", cancellationToken);

    /// <inheritdoc />
    public Task<Guid> CreateOutgoingDraftAsync(
        string accessToken,
        SysmondOutgoingDespatchCreateDto body,
        CancellationToken cancellationToken = default)
        => PostForIdAsync(accessToken, "api/app/outgoing-despatch/draft", body, "outgoing-despatch/draft", cancellationToken);

    /// <inheritdoc />
    public Task<Guid> CreateOutgoingItemAsync(
        string accessToken,
        SysmondDespatchItemCreateDto body,
        CancellationToken cancellationToken = default)
        => PostForIdAsync(accessToken, "api/app/outgoing-despatch/item", body, "outgoing-despatch/item", cancellationToken);

    /// <inheritdoc />
    public Task SaveOutgoingAsync(
        string accessToken,
        SysmondIncomingDespatchSaveDto body,
        CancellationToken cancellationToken = default)
        => PostSaveAsync(accessToken, "api/app/outgoing-despatch/save", body, "outgoing-despatch/save", cancellationToken);

    private async Task PostSaveAsync(
        string accessToken,
        string url,
        SysmondIncomingDespatchSaveDto body,
        string operationName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(body);

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond {operationName} başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        _logger.LogInformation(
            "Sysmond {Operation} OK: CompanyId={CompanyId}, DespatchId={DespatchId}",
            operationName,
            body.CompanyId,
            body.DespatchId);
    }

    private async Task<Guid> PostForIdAsync<T>(
        string accessToken,
        string url,
        T body,
        string operationName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(body);

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond {operationName} başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondIdResult>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException($"Sysmond {operationName} yanıtı boş veya geçersiz.");

        if (parsed.Data is null || parsed.Data.Id == Guid.Empty)
            throw new InvalidOperationException($"Sysmond {operationName} yanıtında id yok.");

        _logger.LogInformation("Sysmond {Operation} OK: Id={Id}", operationName, parsed.Data.Id);
        return parsed.Data.Id;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
