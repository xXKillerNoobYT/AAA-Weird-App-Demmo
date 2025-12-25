using System.Net;
using System.Text.Json;
using CloudWatcher.Controllers;

namespace CloudWatcher.Middleware;

/// <summary>
/// Global error handling middleware that catches unhandled exceptions
/// and returns standardized API error responses.
/// </summary>
public class GlobalErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlerMiddleware> _logger;

    public GlobalErrorHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalErrorHandlerMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode) = GetStatusCodeAndErrorCode(exception);

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = exception.Message,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow,
            Details = new Dictionary<string, object>
            {
                { "exceptionType", exception.GetType().Name },
                { "stackTrace", exception.StackTrace ?? "No stack trace available" }
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, jsonOptions));
    }

    private static (int StatusCode, string ErrorCode) GetStatusCodeAndErrorCode(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "UNAUTHORIZED"),
            ArgumentException or ArgumentNullException => ((int)HttpStatusCode.BadRequest, "INVALID_ARGUMENT"),
            InvalidOperationException => ((int)HttpStatusCode.Conflict, "INVALID_OPERATION"),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "NOT_FOUND"),
            NotImplementedException => ((int)HttpStatusCode.NotImplemented, "NOT_IMPLEMENTED"),
            TimeoutException => ((int)HttpStatusCode.RequestTimeout, "TIMEOUT"),
            _ => ((int)HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
        };
    }
}
