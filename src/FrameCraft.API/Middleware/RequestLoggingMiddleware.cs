using System.Diagnostics;
using Serilog.Context;

namespace FrameCraft.API.Middleware;

/// <summary>
/// HTTP request ve response'ları loglar
/// Performance metrikleri, request detayları ve hata takibi için
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Loglanmaması gereken endpoint'ler
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/healthz",
        "/ready",
        "/swagger",
        "/favicon.ico"
    };

    // Hassas header'lar (loglanmamalı)
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key"
    };

    // Path → Module mapping
    private static readonly Dictionary<string, string> ModuleMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/api/customers", "CRM" },
        { "/api/companies", "CRM" },
        { "/api/frames", "Inventory" },
        { "/api/sales", "Sales" },
        { "/api/auth", "Authentication" },
        { "/api/admin", "Administration" },
        { "/api/users", "UserManagement" },
        { "/api/tenants", "TenantManagement" }
    };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Excluded path'leri atla
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Request bilgilerini topla
        var requestInfo = new
        {
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            ClientIp = GetClientIpAddress(context),
            UserAgent = context.Request.Headers.UserAgent.ToString()
        };

        // Module'ü belirle
        var module = DetermineModule(context.Request.Path);

        // Tüm context bilgilerini LogContext'e ekle
        using (LogContext.PushProperty("Module", module))
        using (LogContext.PushProperty("ClientIp", requestInfo.ClientIp))
        using (LogContext.PushProperty("HttpMethod", requestInfo.Method))
        using (LogContext.PushProperty("RequestPath", requestInfo.Path))
        {
            try
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} started | Client: {ClientIp} | Module: {Module}",
                    requestInfo.Method,
                    requestInfo.Path,
                    requestInfo.ClientIp,
                    module);

                await _next(context);

                stopwatch.Stop();

                // Authentication sonrası user context'i güncelle
                using (PushUserContext(context))
                {
                    var level = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

                    _logger.Log(
                        level,
                        "HTTP {Method} {Path} completed | Status: {StatusCode} | Duration: {Duration}ms | Module: {Module}",
                        requestInfo.Method,
                        requestInfo.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        module);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                using (PushUserContext(context))
                {
                    _logger.LogError(
                        ex,
                        "HTTP {Method} {Path} failed | Duration: {Duration}ms | Module: {Module} | Error: {ErrorMessage}",
                        requestInfo.Method,
                        requestInfo.Path,
                        stopwatch.ElapsedMilliseconds,
                        module,
                        ex.Message);
                }

                throw;
            }
        }
    }

    private static string DetermineModule(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";
        
        foreach (var mapping in ModuleMapping)
        {
            if (pathValue.StartsWith(mapping.Key, StringComparison.OrdinalIgnoreCase))
            {
                return mapping.Value;
            }
        }

        return "General";
    }

    private static bool IsExcludedPath(PathString path)
    {
        return ExcludedPaths.Any(excluded => 
            path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Proxy arkasındaysa X-Forwarded-For header'ını kontrol et
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static IDisposable PushUserContext(HttpContext context)
    {
        var userId = context.User?.FindFirst("UserId")?.Value ?? "anonymous";
        var tenantId = context.User?.FindFirst("TenantId")?.Value ?? "none";
        var isSuperAdmin = context.User?.FindFirst("IsSuperAdmin")?.Value ?? "false";

        return new CompositeDisposable(
            LogContext.PushProperty("UserId", userId),
            LogContext.PushProperty("TenantId", tenantId),
            LogContext.PushProperty("IsSuperAdmin", isSuperAdmin)
        );
    }
}

/// <summary>
/// Birden fazla IDisposable'ı tek seferde dispose etmek için
/// </summary>
internal sealed class CompositeDisposable : IDisposable
{
    private readonly IDisposable[] _disposables;

    public CompositeDisposable(params IDisposable[] disposables)
    {
        _disposables = disposables;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// Extension method for RequestLoggingMiddleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
