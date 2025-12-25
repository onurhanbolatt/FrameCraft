using FrameCraft.Application.Common.Models;
using FrameCraft.Domain.Exceptions;
using Serilog.Context;
using System.Net;
using System.Text.Json;

namespace FrameCraft.API.Middleware;

/// <summary>
/// Global exception handler - Tüm unhandled exception'ları yakalar
/// Structured logging ile detaylı hata kaydı tutar
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        // Exception ID oluştur (hata takibi için)
        var errorId = Guid.NewGuid().ToString("N")[..8];

        using (LogContext.PushProperty("ErrorId", errorId))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            var (statusCode, logLevel, userMessage) = ClassifyException(exception);

            response.StatusCode = statusCode;

            // Log the exception
            _logger.Log(
                logLevel,
                exception,
                "Exception occurred | ErrorId: {ErrorId} | Type: {ExceptionType} | Message: {ExceptionMessage}",
                errorId,
                exception.GetType().Name,
                exception.Message);

            // Response oluştur
            var errorResponse = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = userMessage,
                ErrorId = errorId,
                // Development'ta detayları göster, Production'da gizle
                Details = _environment.IsDevelopment() ? exception.StackTrace : null
            };

            // Validation exception ise hataları ekle
            if (exception is ValidationException validationException)
            {
                errorResponse.Errors = validationException.Errors;
            }

            var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await response.WriteAsync(result);
        }
    }

    private static (int StatusCode, LogLevel Level, string Message) ClassifyException(Exception exception)
    {
        return exception switch
        {
            ValidationException => (
                (int)HttpStatusCode.BadRequest,
                LogLevel.Warning,
                "Doğrulama hatası oluştu. Lütfen girdiğiniz bilgileri kontrol edin."),

            NotFoundException => (
                (int)HttpStatusCode.NotFound,
                LogLevel.Warning,
                exception.Message),

            BadRequestException => (
                (int)HttpStatusCode.BadRequest,
                LogLevel.Warning,
                exception.Message),

            UnauthorizedException => (
                (int)HttpStatusCode.Unauthorized,
                LogLevel.Warning,
                "Bu işlem için yetkiniz bulunmamaktadır."),

            ForbiddenAccessException => (
                (int)HttpStatusCode.Forbidden,
                LogLevel.Warning,
                exception.Message),

            OperationCanceledException => (
                499, // Client Closed Request
                LogLevel.Information,
                "İstek iptal edildi."),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                LogLevel.Error,
                "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.")
        };
    }
}
