using System.Text;
using Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace API;

/// <summary>API katmanı DI genişletmeleri (JWT, Swagger, Sysmond).</summary>
public static class DependencyInjection
{
    /// <summary>JWT Bearer authentication ve authorization kaydı.</summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Sysmondax OAuth options + named HttpClient.
    /// Secret'lar appsettings placeholder; gerçek değerler User Secrets ile:
    /// Sysmond:Username, Sysmond:Password, Sysmond:ClientId, Sysmond:ClientSecret.
    /// </summary>
    public static IServiceCollection AddSysmondIntegration(this IServiceCollection services, IConfiguration configuration)
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

    /// <summary>Swagger UI ve Bearer JWT güvenlik tanımı.</summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Stock Management API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Bearer token. Normal API için Stock Management JWT; " +
                    "Sysmond sync için Auth/sysmond-token'dan alınan Sysmondax access_token."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }
}
