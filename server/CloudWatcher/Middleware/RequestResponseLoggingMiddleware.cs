using System.Diagnostics;
using System.Text;

namespace CloudWatcher.Middleware;

/// <summary>
/// Middleware that logs incoming requests and outgoing responses
/// with performance metrics.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();
        
        // Add correlation ID to response headers
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Log incoming request
        LogRequest(context, correlationId);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // Log outgoing response
            LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private void LogRequest(HttpContext context, string correlationId)
    {
        var request = context.Request;
        
        _logger.LogInformation(
            "HTTP {Method} {Path}{QueryString} | CorrelationId: {CorrelationId} | IP: {RemoteIp}",
            request.Method,
            request.Path,
            request.QueryString,
            correlationId,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    private void LogResponse(HttpContext context, string correlationId, long elapsedMs)
    {
        var logLevel = context.Response.StatusCode >= 400 
            ? LogLevel.Warning 
            : LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMs,
            correlationId);
    }
}
