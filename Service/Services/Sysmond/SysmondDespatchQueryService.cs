using System.Net.Http.Headers;
using System.Text.Json;
using Core.DTOs.Sysmond;
using Core.Services;
using Core.Settings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

/// <summary>Sysmondax <c>/api/app/despatch-query</c> istemcisi.</summary>
public class SysmondDespatchQueryService : ISysmondDespatchQueryService
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SysmondDespatchQueryService> _logger;

    public SysmondDespatchQueryService(
        IHttpClientFactory httpClientFactory,
        ILogger<SysmondDespatchQueryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondDespatchDto>> GetDespatchesAsync(
        string accessToken,
        Guid companyId,
        Guid? companyPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(companyId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var all = new List<SysmondDespatchDto>();
        var skip = 0;

        while (true)
        {
            var url = BuildDespatchesUrl(skip, PageSize, companyId, companyPeriodId);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Sysmond despatch-query/despatches başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
            }

            var page = JsonSerializer.Deserialize<SysmondDespatchPagedResult>(body, JsonOptions)
                ?? throw new InvalidOperationException("Sysmond despatch-query yanıtı boş veya geçersiz.");

            var items = page.Items ?? Array.Empty<SysmondDespatchDto>();
            all.AddRange(items);

            _logger.LogInformation(
                "Sysmond despatch-query sayfa: CompanyId={CompanyId}, Skip={Skip}, Count={Count}, Total={Total}",
                companyId,
                skip,
                items.Count,
                page.TotalCount);

            skip += items.Count;
            if (items.Count == 0 || skip >= page.TotalCount)
                break;
        }

        return all;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondDespatchItemDto>> GetDespatchItemsAsync(
        string accessToken,
        Guid despatchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (despatchId == Guid.Empty)
            throw new ArgumentException("despatchId zorunludur.", nameof(despatchId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/despatch-query/{despatchId}/despatch-items";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond despatch-items başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondDespatchItemListResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond despatch-items yanıtı boş veya geçersiz.");

        var items = parsed.Data ?? Array.Empty<SysmondDespatchItemDto>();
        _logger.LogInformation(
            "Sysmond despatch-items: DespatchId={DespatchId}, Count={Count}",
            despatchId,
            items.Count);
        return items;
    }

    private static string BuildDespatchesUrl(
        int skipCount,
        int maxResultCount,
        Guid companyId,
        Guid? companyPeriodId)
    {
        // CompanyPeriodId opsiyonel bırakıldı ama sync tarafı null geçiyor (tüm dönemler).
        var qs =
            $"api/app/despatch-query/despatches?CompanyId={companyId}&SkipCount={skipCount}&MaxResultCount={maxResultCount}";
        if (companyPeriodId is Guid periodId && periodId != Guid.Empty)
            qs += $"&CompanyPeriodId={periodId}";
        return qs;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
