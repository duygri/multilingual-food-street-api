namespace NarrationApp.Server.Data.Entities;

public sealed class QrCode
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public int TargetId { get; set; }

    public string? LocationHint { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
