using System.Net;

namespace NarrationApp.Server.Services;

public sealed class AuthFlowException(string errorCode, string message, HttpStatusCode statusCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public HttpStatusCode StatusCode { get; } = statusCode;
}
