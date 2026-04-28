using System.Net;

namespace NarrationApp.Mobile.Features.Home;

public sealed class VisitorApiException(string message, HttpStatusCode statusCode, string? errorCode = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public string? ErrorCode { get; } = errorCode;
}
