using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;
using System.Text.Json;

namespace FrameCraft.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour - Command/Query logging
/// Tek sorumluluk: Request/Response loglama
/// </summary>
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    private static readonly Dictionary<string, string> ModuleMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Customer", "CRM" },
        { "Company", "CRM" },
        { "Sale", "Sales" },
        { "Order", "Sales" },
        { "Invoice", "Sales" },
        { "Frame", "Inventory" },
        { "Product", "Inventory" },
        { "Stock", "Inventory" },
        { "Login", "Authentication" },
        { "Logout", "Authentication" },
        { "RefreshToken", "Authentication" },
        { "Password", "Authentication" },
        { "Tenant", "Administration" },
        { "User", "UserManagement" },
        { "Role", "UserManagement" },
        { "File", "FileManagement" },
        { "Upload", "FileManagement" }
    };

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var module = DetermineModule(requestName);

        using (LogContext.PushProperty("Module", module))
        using (LogContext.PushProperty("RequestName", requestName))
        using (LogContext.PushProperty("CqrsRequestId", requestId))
        {
            _logger.LogDebug(
                "CQRS {RequestName} [{RequestId}] starting | Module: {Module} | Request: {@Request}",
                requestName,
                requestId,
                module,
                SanitizeRequest(request));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                stopwatch.Stop();

                _logger.LogDebug(
                    "CQRS {RequestName} [{RequestId}] completed | Module: {Module} | Duration: {Duration}ms",
                    requestName,
                    requestId,
                    module,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    "CQRS {RequestName} [{RequestId}] failed | Module: {Module} | Duration: {Duration}ms | Error: {ErrorMessage}",
                    requestName,
                    requestId,
                    module,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    private static string DetermineModule(string requestName)
    {
        foreach (var mapping in ModuleMapping)
        {
            if (requestName.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                return mapping.Value;
        }
        return "General";
    }

    private static object SanitizeRequest(TRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);

            if (json.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("secret", StringComparison.OrdinalIgnoreCase))
            {
                return new { Type = typeof(TRequest).Name, Note = "[Sensitive data redacted]" };
            }

            return request;
        }
        catch
        {
            return new { Type = typeof(TRequest).Name };
        }
    }
}