namespace NarrationApp.Shared.DTOs.Common;

public sealed class ApiResponse<T>
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    public ErrorResponse? Error { get; init; }
}
