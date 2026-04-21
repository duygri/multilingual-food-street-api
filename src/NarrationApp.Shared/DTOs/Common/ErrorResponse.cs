namespace NarrationApp.Shared.DTOs.Common;

public sealed class ErrorResponse
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();
}
