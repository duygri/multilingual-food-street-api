using System.Net;

namespace NarrationApp.Web.Services;

public sealed class ApiException(string message, HttpStatusCode statusCode, string? errorCode = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public string? ErrorCode { get; } = errorCode;
}
