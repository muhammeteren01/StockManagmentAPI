using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.DTOs.Sysmond;
using Core.Mappings;
using Core.Services;
using Core.Settings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

/// <summary>Sysmondax <c>/api/app/act-query</c> + <c>/api/app/act-address</c> istemcisi.</summary>
public class SysmondActQueryService : ISysmondActQueryService
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SysmondActQueryService> _logger;

    public SysmondActQueryService(
        IHttpClientFactory httpClientFactory,
        ILogger<SysmondActQueryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondActDto>> GetAllActsAsync(
        string accessToken,
        Guid companyId,
        IReadOnlyList<int>? types = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(companyId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var all = new List<SysmondActDto>();
        var skip = 0;

        while (true)
        {
            var url = BuildActQueryUrl(skip, PageSize, companyId, types);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Sysmond act-query başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
            }

            var (items, totalCount) = ParseActQueryPage(body);
            all.AddRange(items);

            _logger.LogInformation(
                "Sysmond act-query sayfa: CompanyId={CompanyId}, Skip={Skip}, Count={Count}, Total={Total}",
                companyId,
                skip,
                items.Count,
                totalCount);

            skip += items.Count;
            if (items.Count == 0 || skip >= totalCount)
                break;
        }

        return all;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateActAsync(
        string accessToken,
        SysmondActCreateDto body,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(body);

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/app/act/act")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond act/create başarısız ({(int)response.StatusCode}): {Truncate(responseBody, 800)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondIdResult>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("Sysmond act/create yanıtı boş veya geçersiz.");

        if (parsed.Data is null || parsed.Data.Id == Guid.Empty)
            throw new InvalidOperationException("Sysmond act/create yanıtında id yok.");

        _logger.LogInformation(
            "Sysmond act/create OK: Id={Id}, Type={Type}, Name={Name}",
            parsed.Data.Id,
            body.Type,
            body.Name);
        return parsed.Data.Id;
    }

    /// <summary>items / data[] / data.items sarmalayıcılarını destekler.</summary>
    private static (IReadOnlyList<SysmondActDto> Items, long TotalCount) ParseActQueryPage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (Array.Empty<SysmondActDto>(), 0);

        var direct = JsonSerializer.Deserialize<SysmondActPagedResult>(body, JsonOptions);
        if (direct?.Items is { Count: > 0 })
            return (direct.Items, direct.TotalCount);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return (direct?.Items ?? Array.Empty<SysmondActDto>(), direct?.TotalCount ?? 0);

        long totalCount = 0;
        if (root.TryGetProperty("totalCount", out var totalEl) && totalEl.TryGetInt64(out var total))
            totalCount = total;

        if (TryGetArrayProperty(root, "data", out var dataArr))
        {
            var items = DeserializeActArray(dataArr);
            return (items, totalCount > 0 ? totalCount : items.Count);
        }

        if (TryGetArrayProperty(root, "items", out var itemsArr))
        {
            var items = DeserializeActArray(itemsArr);
            return (items, totalCount > 0 ? totalCount : items.Count);
        }

        if (root.TryGetProperty("data", out var dataObj) && dataObj.ValueKind == JsonValueKind.Object)
        {
            if (dataObj.TryGetProperty("totalCount", out var innerTotal) && innerTotal.TryGetInt64(out var t2))
                totalCount = t2;
            if (TryGetArrayProperty(dataObj, "items", out var innerItems))
            {
                var items = DeserializeActArray(innerItems);
                return (items, totalCount > 0 ? totalCount : items.Count);
            }
        }

        return (direct?.Items ?? Array.Empty<SysmondActDto>(), direct?.TotalCount ?? 0);
    }

    private static IReadOnlyList<SysmondActDto> DeserializeActArray(JsonElement array) =>
        JsonSerializer.Deserialize<List<SysmondActDto>>(array.GetRawText(), JsonOptions)
        ?? [];

    private static bool TryGetArrayProperty(
        JsonElement obj,
        string name,
        out JsonElement array)
    {
        array = default;
        foreach (var prop in obj.EnumerateObject())
        {
            if (!prop.NameEquals(name) && !string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                array = prop.Value;
                return true;
            }

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                if (TryGetArrayProperty(prop.Value, "items", out array)
                    || TryGetArrayProperty(prop.Value, "data", out array))
                    return true;
            }

            return false;
        }

        return false;
    }

    /// <inheritdoc />
    public Task<SysmondActAddressDebugResult> GetActAddressesDebugAsync(
        string accessToken,
        Guid actId,
        Guid companyId,
        bool includeDisabled = true,
        CancellationToken cancellationToken = default)
        => FetchActAddressesAsync(accessToken, actId, companyId, includeDisabled, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondActAddressDto>> GetActAddressesAsync(
        string accessToken,
        Guid actId,
        Guid companyId,
        bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var result = await FetchActAddressesAsync(accessToken, actId, companyId, includeDisabled, cancellationToken);
        return result.Items;
    }

    /// <inheritdoc />
    public async Task<SysmondActDto?> GetActByIdAsync(
        string accessToken,
        Guid actId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (actId == Guid.Empty)
            throw new ArgumentException("actId zorunludur.", nameof(actId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/act-query/{actId:D}/by-id?includeBalances=false";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Sysmond act-query by-id başarısız: ActId={ActId}, Status={Status}, Body={Body}",
                actId,
                (int)response.StatusCode,
                Truncate(body, 400));
            return null;
        }

        var parsed = JsonSerializer.Deserialize<SysmondApiResult<SysmondActDto>>(body, JsonOptions);
        return parsed?.Data;
    }

    private async Task<SysmondActAddressDebugResult> FetchActAddressesAsync(
        string accessToken,
        Guid actId,
        Guid companyId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (actId == Guid.Empty)
            throw new ArgumentException("actId zorunludur.", nameof(actId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var attemptedUrls = new List<string>();
        SysmondActAddressDebugResult? lastDebug = null;

        foreach (var url in BuildActAddressUrls(actId, companyId, includeDisabled))
        {
            attemptedUrls.Add(url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            lastDebug = new SysmondActAddressDebugResult
            {
                HttpStatusCode = (int)response.StatusCode,
                RawBody = body,
                RequestUrl = url,
                AttemptedUrls = attemptedUrls
            };

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound
                || IsNotFoundBusinessError(response.StatusCode, body))
            {
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Sysmond act-address başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
            }

            var items = SysmondActAddressResponseParser.Parse(body);
            lastDebug.Items = items;
            lastDebug.ParsedCount = items.Count;

            if (items.Count > 0)
            {
                _logger.LogInformation(
                    "Sysmond act-address: ActId={ActId}, Count={Count}, Url={Url}",
                    actId,
                    items.Count,
                    url);
                lastDebug.AttemptedUrls = attemptedUrls;
                return lastDebug;
            }
        }

        var actDetail = await GetActByIdAsync(accessToken, actId, cancellationToken);
        var debug = lastDebug ?? new SysmondActAddressDebugResult { AttemptedUrls = attemptedUrls };
        debug.AttemptedUrls = attemptedUrls;
        debug.CanAccessAddressAndContactInfo = actDetail?.CanAccessAddressAndContactInfo;

        if (debug.ParsedCount == 0 && actDetail?.CanAccessAddressAndContactInfo == false)
        {
            _logger.LogWarning(
                "Sysmond act-address boş; integration kullanıcısında adres yetkisi yok olabilir: ActId={ActId}",
                actId);
        }
        else if (debug.ParsedCount == 0)
        {
            _logger.LogWarning(
                "Sysmond act-address tüm URL'lerde boş: ActId={ActId}, Body={Body}",
                actId,
                Truncate(debug.RawBody, 800));
        }

        return debug;
    }

    /// <summary>
    /// Sandbox'ta çalışan format önce: <c>includeDisabled=false</c>, sonra parametresiz, sonra true.
    /// </summary>
    private static IEnumerable<string> BuildActAddressUrls(Guid actId, Guid companyId, bool includeDisabled)
    {
        var id = actId.ToString("D");
        var disabledFlags = includeDisabled
            ? new string?[] { "false", null, "true" }
            : new string?[] { "false", null, "true" };

        foreach (var flag in disabledFlags)
        {
            var suffix = flag is null ? string.Empty : $"&includeDisabled={flag}";
            yield return $"api/app/act-address?actId={id}{suffix}";

            if (companyId != Guid.Empty)
            {
                var company = companyId.ToString("D");
                yield return $"api/app/act-address?actId={id}&CompanyId={company}{suffix}";
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondDespatchScenarioTypeMapDto>> GetDespatchScenariosByActIdAsync(
        string accessToken,
        Guid actId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (actId == Guid.Empty)
            throw new ArgumentException("actId zorunludur.", nameof(actId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var url = $"api/app/act-query/{actId:D}/despatch-scenarios-by-act-id";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Sysmond despatch-scenarios-by-act-id başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<SysmondDespatchScenarioListResult>(body, JsonOptions);
        var items = parsed?.Data ?? Array.Empty<SysmondDespatchScenarioTypeMapDto>();
        _logger.LogInformation(
            "Sysmond despatch-scenarios: ActId={ActId}, Count={Count}",
            actId,
            items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysmondCompanyDocNoTemplateDto>> GetCompanyDocNoTemplatesAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (companyId == Guid.Empty)
            throw new ArgumentException("companyId zorunludur.", nameof(companyId));

        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);
        var all = new List<SysmondCompanyDocNoTemplateDto>();
        var skip = 0;
        const int pageSize = 50;

        while (true)
        {
            var url =
                $"api/app/company-invoice-template?CompanyId={companyId}&SkipCount={skip}&MaxResultCount={pageSize}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Sysmond company-invoice-template başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");
            }

            var page = JsonSerializer.Deserialize<SysmondCompanyDocNoTemplatePagedResult>(body, JsonOptions)
                ?? throw new InvalidOperationException("Sysmond company-invoice-template yanıtı boş.");

            var items = page.Items ?? Array.Empty<SysmondCompanyDocNoTemplateDto>();
            all.AddRange(items);

            skip += items.Count;
            if (items.Count == 0 || skip >= page.TotalCount)
                break;
        }

        _logger.LogInformation(
            "Sysmond company templates: CompanyId={CompanyId}, Count={Count}",
            companyId,
            all.Count);
        return all;
    }

    private static string BuildActQueryUrl(
        int skip,
        int max,
        Guid companyId,
        IReadOnlyList<int>? types)
    {
        var url =
            $"api/app/act-query?CompanyId={companyId}&SkipCount={skip}&MaxResultCount={max}&IncludeBalances=false";

        if (types is { Count: > 0 })
        {
            foreach (var type in types)
                url += $"&Types={type}";
        }

        return url;
    }

    private static bool IsNotFoundBusinessError(System.Net.HttpStatusCode statusCode, string body)
    {
        if (statusCode != System.Net.HttpStatusCode.Forbidden)
            return false;

        return body.Contains("Sysmond.Error:50001", StringComparison.Ordinal)
               || body.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";
}
