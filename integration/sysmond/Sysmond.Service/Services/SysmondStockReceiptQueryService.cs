using System.Net.Http.Headers;
using System.Text.Json;
using Integration.Sysmond.Core.DTOs.StockReceipts;
using Integration.Sysmond.Core.Services;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

/// <summary>Sysmondax <c>/api/app/stock-receipt</c> istemcisi.</summary>
public class SysmondStockReceiptQueryService : ISysmondStockReceiptQueryService
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SysmondStockReceiptQueryService> _logger;

    public SysmondStockReceiptQueryService(
        IHttpClientFactory httpClientFactory,
        ILogger<SysmondStockReceiptQueryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondStockReceiptDto>> GetStockReceiptsAsync(
        string accessToken,
        Guid companyPeriodId,
        int? type = null,
        bool? isDraft = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyPeriodId == Guid.Empty)
            throw new ArgumentException("companyPeriodId zorunludur.", nameof(companyPeriodId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var all = new List<SysmondStockReceiptDto>();
        var skip = 0;

        while (true)
        {
            var url = BuildListUrl(companyPeriodId, type, isDraft, skip, PageSize);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Sysmond stock-receipt-list başarısız: {Status} {Body}",
                    (int)response.StatusCode,
                    body);
                response.EnsureSuccessStatusCode();
            }

            var parsed = JsonSerializer.Deserialize<SysmondStockReceiptPagedResult>(body, JsonOptions)
                ?? new SysmondStockReceiptPagedResult();
            var page = parsed.Items ?? Array.Empty<SysmondStockReceiptDto>();
            all.AddRange(page);

            _logger.LogInformation(
                "Sysmond stock-receipt-list: Period={PeriodId}, Type={Type}, Skip={Skip}, Count={Count}, Total={Total}",
                companyPeriodId,
                type,
                skip,
                page.Count,
                parsed.TotalCount);

            skip += page.Count;
            if (page.Count == 0 || skip >= parsed.TotalCount)
                break;
        }

        return all;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondStockReceiptItemDto>> GetStockReceiptItemsAsync(
        string accessToken,
        Guid stockReceiptId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (stockReceiptId == Guid.Empty)
            throw new ArgumentException("stockReceiptId zorunludur.", nameof(stockReceiptId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/stock-receipt/{stockReceiptId}/stock-receipt-item-list";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Sysmond stock-receipt-item-list başarısız: ReceiptId={ReceiptId}, {Status} {Body}",
                stockReceiptId,
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }

        var parsed = JsonSerializer.Deserialize<SysmondStockReceiptItemListResult>(body, JsonOptions)
            ?? new SysmondStockReceiptItemListResult();
        return parsed.Data ?? Array.Empty<SysmondStockReceiptItemDto>();
    }

    private static string BuildListUrl(
        Guid companyPeriodId,
        int? type,
        bool? isDraft,
        int skip,
        int max)
    {
        var qs = $"CompanyPeriodId={companyPeriodId}&SkipCount={skip}&MaxResultCount={max}";
        if (type is int t)
            qs += $"&Type={t}";
        if (isDraft is bool draft)
            qs += $"&IsDraft={(draft ? "true" : "false")}";
        return $"api/app/stock-receipt/stock-receipt-list?{qs}";
    }
}
