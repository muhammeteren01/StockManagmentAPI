using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Integration.Sysmond.Core.DTOs;
using Integration.Sysmond.Core.Services;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

/// <summary>Sysmondax warehouse, warehouse-stock ve stock-balance istemcisi.</summary>
public class SysmondInventoryQueryService : ISysmondInventoryQueryService
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
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
        // includeDeactivated=true sandbox'ta boş data döndürebiliyor; yalnızca companyId ile çağır.
        var url = $"api/app/warehouse?companyId={companyId}";

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
    public async Task<IReadOnlyList<SysmondCompanyPeriodDto>> GetMyCompanyPeriodsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        // isActive query'si bazı ortamlarda listeyi bozabiliyor; hepsini alıp caller filtreler.
        var url = "api/app/user-profile/my-company-periods";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond my-company-periods başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondCompanyPeriodListResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond my-company-periods yanıtı boş veya geçersiz.");

        var items = parsed.Data ?? Array.Empty<SysmondCompanyPeriodDto>();
        _logger.LogInformation("Sysmond my-company-periods: Count={Count}", items.Count);
        return items;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesByStockAsync(
        string accessToken,
        Guid companyPeriodId,
        Guid stockId,
        CancellationToken cancellationToken = default)
    {
        if (stockId == Guid.Empty)
            throw new ArgumentException("stockId zorunludur.", nameof(stockId));

        return GetStockBalancesAsync(
            accessToken,
            companyPeriodId,
            warehouseId: null,
            stockId: stockId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesByWarehouseAsync(
        string accessToken,
        Guid companyPeriodId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("warehouseId zorunludur.", nameof(warehouseId));

        return GetStockBalancesAsync(
            accessToken,
            companyPeriodId,
            warehouseId: warehouseId,
            stockId: null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteWarehouseAsync(
        string accessToken,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("warehouseId zorunludur.", nameof(warehouseId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/app/warehouse/{warehouseId:D}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond warehouse/delete başarısız ({(int)response.StatusCode}): {Truncate(body, 800)}");
        }

        _logger.LogInformation("Sysmond warehouse/delete OK: WarehouseId={WarehouseId}", warehouseId);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateWarehouseAsync(
        string accessToken,
        SysmondWarehouseCreateDto body,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(body);
        if (body.CompanyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(body));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/app/warehouse")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond warehouse/create başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondIdResult>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond warehouse/create yanıtı boş veya geçersiz.");

        if (parsed.Data is null || parsed.Data.Id == Guid.Empty)
            throw new InvalidOperationException("Sysmond warehouse/create yanıtında id yok.");

        _logger.LogInformation(
            "Sysmond warehouse/create OK: CompanyId={CompanyId}, WarehouseId={WarehouseId}, Name={Name}",
            body.CompanyId,
            parsed.Data.Id,
            body.Name);

        return parsed.Data.Id;
    }

    /// <inheritdoc />
    public async Task UpdateWarehouseAsync(
        string accessToken,
        SysmondWarehouseUpdateDto body,
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

        using var request = new HttpRequestMessage(HttpMethod.Put, "api/app/warehouse")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond warehouse/update başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        _logger.LogInformation(
            "Sysmond warehouse/update OK: Id={Id}, Name={Name}",
            body.Id,
            body.Name);
    }

    private async Task<IReadOnlyList<SysmondStockBalanceDto>> GetStockBalancesAsync(
        string accessToken,
        Guid companyPeriodId,
        Guid? warehouseId,
        Guid? stockId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyPeriodId == Guid.Empty)
            throw new ArgumentException("companyPeriodId zorunludur.", nameof(companyPeriodId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var all = new List<SysmondStockBalanceDto>();
        var skip = 0;

        while (true)
        {
            var url =
                $"api/app/stock/balance?CompanyPeriodId={companyPeriodId}&SkipCount={skip}&MaxResultCount={PageSize}";
            if (warehouseId is Guid wh)
                url += $"&WarehouseId={wh}";
            if (stockId is Guid st)
                url += $"&StockId={st}";

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
                "Sysmond stock/balance: CompanyPeriodId={CompanyPeriodId}, WarehouseId={WarehouseId}, StockId={StockId}, Skip={Skip}, Count={Count}, Total={Total}",
                companyPeriodId,
                warehouseId,
                stockId,
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
