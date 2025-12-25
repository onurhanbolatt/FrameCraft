using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FrameCraft.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour - Performance monitoring
/// Tek sorumluluk: Yavaş request'leri tespit et ve uyar
/// </summary>
public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;

    private const int WarningThresholdMs = 500;
    private const int ErrorThresholdMs = 2000;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs >= ErrorThresholdMs)
        {
            _logger.LogError(
                "PERFORMANCE CRITICAL: {RequestName} took {Duration}ms (threshold: {Threshold}ms)",
                typeof(TRequest).Name,
                elapsedMs,
                ErrorThresholdMs);
        }
        else if (elapsedMs >= WarningThresholdMs)
        {
            _logger.LogWarning(
                "PERFORMANCE WARNING: {RequestName} took {Duration}ms (threshold: {Threshold}ms)",
                typeof(TRequest).Name,
                elapsedMs,
                WarningThresholdMs);
        }

        return response;
    }
}