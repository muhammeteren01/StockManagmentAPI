using System.Security.Claims;
using Serilog;
using Serilog.Events;

namespace API.Middleware;

/// <summary>Logging middleware kayıt yardımcıları.</summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>
    /// Serilog HTTP request logging.
    /// Pipeline: CorrelationId → RequestLogging → ExceptionHandler (dıştan içe).
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
            {
                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
                    return LogEventLevel.Verbose;

                if (exception is not null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("CorrelationId",
                    httpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                    ?? httpContext.TraceIdentifier);

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId",
                        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                    diagnosticContext.Set("UserRole",
                        httpContext.User.FindFirstValue(ClaimTypes.Role));
                    diagnosticContext.Set("CompanyId",
                        httpContext.User.FindFirstValue("company_id"));
                }
            };
        });
    }
}
