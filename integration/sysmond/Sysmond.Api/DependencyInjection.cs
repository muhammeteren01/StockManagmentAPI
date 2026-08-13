using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Integration.Sysmond.Api;

/// <summary>Sysmond integration DI (options + named HttpClient).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Sysmondax OAuth options + named HttpClient.
    /// Secret'lar appsettings placeholder; gerçek değerler User Secrets ile:
    /// Sysmond:Username, Sysmond:Password, Sysmond:ClientId, Sysmond:ClientSecret.
    /// </summary>
    public static IServiceCollection AddSysmondIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SysmondOptions>(configuration.GetSection(SysmondOptions.SectionName));
        services.PostConfigure<SysmondOptions>(options =>
        {
            options.BaseUrl = options.BaseUrl?.Trim().TrimEnd('/') ?? string.Empty;
            options.Username = options.Username?.Trim() ?? string.Empty;
            options.Password = options.Password?.Trim() ?? string.Empty;
            options.ClientId = options.ClientId?.Trim() ?? string.Empty;
            options.ClientSecret = options.ClientSecret?.Trim() ?? string.Empty;
            options.Scope = options.Scope?.Trim() ?? string.Empty;
        });

        services.AddHttpClient(SysmondOptions.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SysmondOptions>>().Value;

            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.sysmondax.com"
                : options.BaseUrl.TrimEnd('/');

            client.BaseAddress = new Uri(baseUrl + "/");
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
