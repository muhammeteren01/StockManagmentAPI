using System.Diagnostics;
using Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Core.Validations.ValidationException;

namespace API.Middleware;

/// <summary>
/// Yakalanmamış exception'ları RFC7807 ProblemDetails yanıtına çevirir.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "İşlenmeyen hata: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "İş kuralı / istemci hatası ({StatusCode}): {Message}", statusCode, exception.Message);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private (int StatusCode, string Title, string? Detail, IDictionary<string, string[]>? Errors) Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Doğrulama hatası",
                validation.Message,
                validation.Errors),

            UnauthorizedException unauthorized => (
                StatusCodes.Status401Unauthorized,
                "Yetkisiz",
                unauthorized.Message,
                null),

            ForbiddenException forbidden => (
                StatusCodes.Status403Forbidden,
                "Erişim reddedildi",
                forbidden.Message,
                null),

            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Çakışma",
                conflict.Message,
                null),

            KeyNotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Bulunamadı",
                notFound.Message,
                null),

            InvalidOperationException invalid => (
                StatusCodes.Status400BadRequest,
                "İş kuralı ihlali",
                invalid.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Sunucu hatası",
                _environment.IsDevelopment() ? exception.Message : "Beklenmeyen bir hata oluştu.",
                null)
        };
    }
}
