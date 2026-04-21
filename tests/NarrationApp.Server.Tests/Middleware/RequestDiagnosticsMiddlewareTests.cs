using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Middleware;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;

namespace NarrationApp.Server.Tests.Middleware;

public sealed class RequestDiagnosticsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_reuses_incoming_correlation_id_and_logs_completed_request()
    {
        var logger = new TestLogger<RequestDiagnosticsMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/pois";
        context.Request.Headers[AppConstants.CorrelationIdHeaderName] = "corr-123";
        context.Items[AppConstants.HttpContextUserIdKey] = "user-1";
        context.Items[AppConstants.HttpContextUserRoleKey] = "admin";

        var sut = new RequestDiagnosticsMiddleware(
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                await Task.CompletedTask;
            },
            logger,
            Options.Create(new RequestDiagnosticsOptions { SlowRequestThresholdMs = 10_000 }));

        await sut.InvokeAsync(context);

        Assert.Equal("corr-123", context.TraceIdentifier);
        Assert.Equal("corr-123", context.Response.Headers[AppConstants.CorrelationIdHeaderName].ToString());

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal("corr-123", scope["CorrelationId"]);
        Assert.Equal("user-1", scope["UserId"]);
        Assert.Equal("admin", scope["UserRole"]);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Equal("GET", entry.State["Method"]);
        Assert.Equal("/api/pois", entry.State["Path"]);
        Assert.Equal(StatusCodes.Status200OK, entry.State["StatusCode"]);
    }

    [Fact]
    public async Task InvokeAsync_generates_correlation_id_and_warns_for_slow_request()
    {
        var logger = new TestLogger<RequestDiagnosticsMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/audio/tts";

        var sut = new RequestDiagnosticsMiddleware(
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                await Task.Delay(5);
            },
            logger,
            Options.Create(new RequestDiagnosticsOptions { SlowRequestThresholdMs = 0 }));

        await sut.InvokeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers[AppConstants.CorrelationIdHeaderName].ToString()));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
        Assert.Equal(StatusCodes.Status200OK, entry.State["StatusCode"]);
    }

    [Fact]
    public async Task InvokeAsync_logs_error_and_rethrows_unhandled_exception()
    {
        var logger = new TestLogger<RequestDiagnosticsMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/failing";

        var sut = new RequestDiagnosticsMiddleware(
            _ => throw new InvalidOperationException("boom"),
            logger,
            Options.Create(new RequestDiagnosticsOptions { SlowRequestThresholdMs = 10_000 }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.InvokeAsync(context));

        Assert.Equal("boom", exception.Message);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Same(exception, entry.Exception);
        Assert.Equal("/api/failing", entry.State["Path"]);
    }
}
