using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.Core;
using Serilog.Context;
using System.Security.Claims;

namespace FrameCraft.API.Middleware;

/// <summary>
/// Her HTTP isteğinde tenant'ı belirleyen middleware
/// Tenant belirleme önceliği:
/// 1. X-Tenant-Id header (SuperAdmin için)
/// 2. JWT token'daki TenantId claim
/// 3. Subdomain (ileride eklenebilir)
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantRepository tenantRepository)
    {
        // User ve Tenant bilgilerini al (log context için)
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var tenantId = "none";

        // Anonim endpoint'leri atla (login, register vb.)
        if (IsAnonymousEndpoint(context.Request.Path))
        {
            using (LogContext.PushProperty("TenantId", tenantId))
            using (LogContext.PushProperty("UserId", userId))
            {
                await _next(context);
            }
            return;
        }

        // Kullanıcı authenticated değilse devam et
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            using (LogContext.PushProperty("TenantId", tenantId))
            using (LogContext.PushProperty("UserId", userId))
            {
                await _next(context);
            }
            return;
        }

        // SuperAdmin kontrolü
        var isSuperAdminClaim = context.User.FindFirst("IsSuperAdmin")?.Value;
        var isSuperAdmin = isSuperAdminClaim == "True" || isSuperAdminClaim == "true";
        tenantContext.SetSuperAdminMode(isSuperAdmin);

        // SuperAdmin için X-Tenant-Id header kontrolü
        if (isSuperAdmin && context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var headerTenantId))
            {
                var tenant = await tenantRepository.GetByIdAsync(headerTenantId, context.RequestAborted);
                if (tenant != null && !tenant.IsDeleted)
                {
                    tenantContext.SwitchToTenant(headerTenantId);
                    tenantId = headerTenantId.ToString();
                    _logger.LogDebug("SuperAdmin tenant'a geçiş yaptı: {TenantId}", headerTenantId);
                }
            }
        }

        // JWT token'daki TenantId claim'i
        var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tokenTenantId))
        {
            var tenant = await tenantRepository.GetByIdAsync(tokenTenantId, context.RequestAborted);
            if (tenant != null && !tenant.IsDeleted)
            {
                tenantContext.SetTenant(tokenTenantId, tenant.Subdomain);
                tenantId = tokenTenantId.ToString();
                _logger.LogDebug("Tenant context ayarlandı: {TenantId} ({Subdomain})", tokenTenantId, tenant.Subdomain);
            }
        }

        // Serilog LogContext'e push et - tüm loglar bu bilgileri içerecek
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }

    private static bool IsAnonymousEndpoint(PathString path)
    {
        var anonymousPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/register",
            "/swagger",
            "/health"
        };

        return anonymousPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Middleware extension method
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}