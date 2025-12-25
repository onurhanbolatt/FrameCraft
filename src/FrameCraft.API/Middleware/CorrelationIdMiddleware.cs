using Serilog.Context;

namespace FrameCraft.API.Middleware;

/// <summary>
/// Her HTTP isteğine benzersiz bir Correlation ID atar
/// Distributed tracing ve log takibi için kritik
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Gelen request'te correlation ID var mı kontrol et
        var correlationId = GetOrCreateCorrelationId(context);

        // Response header'a ekle (client'ın görebilmesi için)
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
            return Task.CompletedTask;
        });

        // Serilog LogContext'e correlation ID'yi ekle
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Önce header'dan kontrol et (microservice'lerden gelen request'ler için)
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId!;
        }

        // Yoksa yeni bir tane oluştur
        return Guid.NewGuid().ToString("N")[..12]; // Kısa format: 12 karakter
    }
}

/// <summary>
/// Extension method for CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
