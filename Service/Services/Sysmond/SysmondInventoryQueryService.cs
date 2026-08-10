using System.Net.Http.Headers;
using System.Text.Json;
using Core.DTOs.Sysmond;
using Core.Services;
using Core.Settings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

/// <summary>Sysmondax warehouse, warehouse-stock ve stock-balance istemcisi.</summary>
public class SysmondInventoryQueryService : ISysmondInventoryQueryService
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SysmondInventoryQueryService> _logger;

    public SysmondInventoryQueryService(
        IHttpClientFactory httpClientFactory,
        ILogger<SysmondInventoryQueryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondWarehouseDto>> GetWarehousesAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(companyId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/warehouse?companyId={companyId}&includeDeactivated=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond warehouse başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondWarehouseListResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond warehouse yanıtı boş veya geçersiz.");

        var items = parsed.Data ?? Array.Empty<SysmondWarehouseDto>();
        _logger.LogInformation("Sysmond warehouse: CompanyId={CompanyId}, Count={Count}", companyId, items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondWarehouseStockDto>> GetWarehouseStocksAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(companyId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/warehouse-stock?CompanyId={companyId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond warehouse-stock başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondWarehouseStockListResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond warehouse-stock yanıtı boş veya geçersiz.");

        var items = parsed.Data ?? Array.Empty<SysmondWarehouseStockDto>();
        _logger.LogInformation(
            "Sysmond warehouse-stock: CompanyId={CompanyId}, Count={Count}",
            companyId,
            items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesByWarehouseAsync(
        string accessToken,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("warehouseId zorunludur.", nameof(warehouseId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var all = new List<SysmondStockBalanceDto>();
        var skip = 0;

        while (true)
        {
            var url =
                $"api/app/stock/balance?WarehouseId={warehouseId}&SkipCount={skip}&MaxResultCount={PageSize}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Sysmond stock/balance başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
            }

            var page = JsonSerializer.Deserialize<SysmondStockBalancePagedResult>(body, JsonOptions)
                ?? throw new InvalidOperationException("Sysmond stock/balance yanıtı boş veya geçersiz.");

            var items = page.Items ?? Array.Empty<SysmondStockBalanceDto>();
            all.AddRange(items);

            _logger.LogInformation(
                "Sysmond stock/balance: WarehouseId={WarehouseId}, Skip={Skip}, Count={Count}, Total={Total}",
                warehouseId,
                skip,
                items.Count,
                page.TotalCount);

            skip += items.Count;
            if (items.Count == 0 || skip >= page.TotalCount)
                break;
        }

        return all;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
