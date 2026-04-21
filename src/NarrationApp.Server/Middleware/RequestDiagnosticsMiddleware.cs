using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Shared.Constants;

namespace NarrationApp.Server.Middleware;

public sealed class RequestDiagnosticsMiddleware(
    RequestDelegate next,
    ILogger<RequestDiagnosticsMiddleware> logger,
    IOptions<RequestDiagnosticsOptions> options)
{
    private readonly RequestDiagnosticsOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[AppConstants.CorrelationIdHeaderName] = correlationId;

        var userId = context.Items[AppConstants.HttpContextUserIdKey]?.ToString()
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = context.Items[AppConstants.HttpContextUserRoleKey]?.ToString()
            ?? context.User.FindFirstValue(ClaimTypes.Role);

        using var _ = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = context.TraceIdentifier,
            ["UserId"] = userId,
            ["UserRole"] = userRole
        });

        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            logger.LogError(
                exception,
                "Unhandled exception for HTTP {Method} {Path} after {ElapsedMs} ms.",
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                Math.Round(elapsedMs, 2));
            throw;
        }

        var completedElapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        var endpointName = context.GetEndpoint()?.DisplayName ?? "(unmatched)";
        var logLevel = GetLogLevel(context.Response.StatusCode, completedElapsedMs, _options.SlowRequestThresholdMs);

        logger.Log(
            logLevel,
            "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs} ms for {EndpointName}.",
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Response.StatusCode,
            Math.Round(completedElapsedMs, 2),
            endpointName);
    }

    private static LogLevel GetLogLevel(int statusCode, double elapsedMs, int slowRequestThresholdMs)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        if (statusCode >= StatusCodes.Status400BadRequest || elapsedMs >= slowRequestThresholdMs)
        {
            return LogLevel.Warning;
        }

        return LogLevel.Information;
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incomingHeader = context.Request.Headers[AppConstants.CorrelationIdHeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(incomingHeader))
        {
            return incomingHeader.Trim();
        }

        return string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : context.TraceIdentifier;
    }
}
