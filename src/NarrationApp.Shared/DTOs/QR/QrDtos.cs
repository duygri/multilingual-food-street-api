namespace NarrationApp.Shared.DTOs.QR;

public sealed class QrCodeDto
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string TargetType { get; init; } = string.Empty;

    public int TargetId { get; init; }

    public string? LocationHint { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    public string? PublicUrl { get; init; }

    public string? AppDeepLink { get; init; }
}

public sealed class CreateQrRequest
{
    public string TargetType { get; init; } = string.Empty;

    public int TargetId { get; init; }

    public string? LocationHint { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }
}
