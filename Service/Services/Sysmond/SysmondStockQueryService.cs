using System.Net.Http.Headers;

using System.Text.Json;

using Core.DTOs.Sysmond;

using Core.Services;

using Core.Settings;

using Microsoft.Extensions.Logging;



namespace Service.Services.Sysmond;



/// <summary>Sysmondax <c>/api/app/stock-query</c> istemcisi.</summary>

public class SysmondStockQueryService : ISysmondStockQueryService

{

    private const int PageSize = 100;



    private static readonly JsonSerializerOptions JsonOptions = new()

    {

        PropertyNameCaseInsensitive = true

    };



    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<SysmondStockQueryService> _logger;



    public SysmondStockQueryService(

        IHttpClientFactory httpClientFactory,

        ILogger<SysmondStockQueryService> logger)

    {

        _httpClientFactory = httpClientFactory;

        _logger = logger;

    }



    /// <inheritdoc />

    public async Task<IReadOnlyList<SysmondStockDto>> GetAllStocksAsync(

        string accessToken,

        Guid? companyId = null,

        CancellationToken cancellationToken = default)

    {

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);



        var client = _httpClientFactory.CreateClient(SysmondOptions.HttpClientName);



        var all = new List<SysmondStockDto>();

        var skip = 0;



        while (true)

        {

            var url = BuildUrl(skip, PageSize, companyId);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);



            using var response = await client.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);



            if (!response.IsSuccessStatusCode)

            {

                throw new InvalidOperationException(

                    $"Sysmond stock-query başarısız ({(int)response.StatusCode}): {Truncate(body, 500)}");

            }



            var page = JsonSerializer.Deserialize<SysmondStockPagedResult>(body, JsonOptions)

                ?? throw new InvalidOperationException("Sysmond stock-query yanıtı boş veya geçersiz.");



            var items = page.Items ?? Array.Empty<SysmondStockDto>();

            all.AddRange(items);



            _logger.LogInformation(

                "Sysmond stock-query sayfa: Skip={Skip}, Count={Count}, Total={Total}",

                skip, items.Count, page.TotalCount);



            skip += items.Count;

            if (items.Count == 0 || skip >= page.TotalCount)

                break;

        }



        return all;

    }



    private static string BuildUrl(int skipCount, int maxResultCount, Guid? companyId)

    {

        var qs = $"api/app/stock-query?IncludePrice=true&SkipCount={skipCount}&MaxResultCount={maxResultCount}";

        if (companyId is Guid cid && cid != Guid.Empty)

            qs += $"&CompanyId={cid}";

        return qs;

    }



    private static string Truncate(string value, int maxLength)

        => value.Length <= maxLength ? value : value[..maxLength] + "...";

}


