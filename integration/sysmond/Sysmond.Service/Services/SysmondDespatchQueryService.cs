using System.Net.Http.Headers;
using System.Text.Json;
using Integration.Sysmond.Core.DTOs;
using Integration.Sysmond.Core.Services;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

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

    /// <inheritdoc />
    public async Task<SysmondDespatchDeliveryAddressDto?> GetDespatchDeliveryAddressAsync(
        string accessToken,
        Guid despatchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (despatchId == Guid.Empty)
            throw new ArgumentException("despatchId zorunludur.", nameof(despatchId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/despatch-query/{despatchId}/despatch-delivery-address";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound
            || IsDeliveryAddressMissing(response.StatusCode, body))
        {
            _logger.LogInformation(
                "Sysmond despatch-delivery-address yok: DespatchId={DespatchId}, Status={Status}",
                despatchId,
                (int)response.StatusCode);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond despatch-delivery-address başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondDespatchDeliveryAddressResult>(body, JsonOptions);
        var data = parsed?.Data;
        _logger.LogInformation(
            "Sysmond despatch-delivery-address: DespatchId={DespatchId}, HasAddress={HasAddress}, AddressId={AddressId}",
            despatchId,
            data is not null,
            data?.Id);
        return data;
    }

    /// <inheritdoc />
    public async Task<SysmondCompanyAddressDto?> GetCompanyAddressByIdAsync(
        string accessToken,
        Guid companyAddressId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyAddressId == Guid.Empty)
            throw new ArgumentException("companyAddressId zorunludur.", nameof(companyAddressId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/company-address/{companyAddressId}/address-by-id";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound
            || IsAddressNotFoundBusinessError(response.StatusCode, body))
        {
            _logger.LogInformation(
                "Sysmond company-address yok: AddressId={AddressId}, Status={Status}",
                companyAddressId,
                (int)response.StatusCode);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond company-address başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondCompanyAddressResult>(body, JsonOptions);
        var data = parsed?.Data;
        _logger.LogInformation(
            "Sysmond company-address: AddressId={AddressId}, HasAddress={HasAddress}, Street={Street}",
            companyAddressId,
            data is not null,
            data?.Street);
        return data;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondDespatchPartyDto>> GetDespatchPartiesAsync(
        string accessToken,
        Guid companyId,
        Guid despatchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(companyId));
        if (despatchId == Guid.Empty)
            throw new ArgumentException("despatchId zorunludur.", nameof(despatchId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/despatch-party?companyId={companyId}&despatchId={despatchId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond despatch-party başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondDespatchPartyListResult>(body, JsonOptions);
        var items = parsed?.Data ?? Array.Empty<SysmondDespatchPartyDto>();
        _logger.LogInformation(
            "Sysmond despatch-party: DespatchId={DespatchId}, Count={Count}",
            despatchId,
            items.Count);
        return items;
    }

    /// <summary>
    /// Sysmond çoğu “bulunamadı” iş kuralını 403 + <c>Sysmond.Error:50001</c> ile döner (HTTP 404 değil).
    /// </summary>
    private static bool IsDeliveryAddressMissing(System.Net.HttpStatusCode statusCode, string body)
        => IsAddressNotFoundBusinessError(statusCode, body)
           || (statusCode == System.Net.HttpStatusCode.Forbidden
               && body.Contains("teslimat adresi bulunamadı", StringComparison.OrdinalIgnoreCase));

    private static bool IsAddressNotFoundBusinessError(System.Net.HttpStatusCode statusCode, string body)
    {
        if (statusCode != System.Net.HttpStatusCode.Forbidden)
            return false;

        return body.Contains("Sysmond.Error:50001", StringComparison.Ordinal)
               || body.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase);
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
