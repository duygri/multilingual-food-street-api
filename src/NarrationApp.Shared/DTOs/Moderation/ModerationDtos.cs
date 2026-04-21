using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Moderation;

public sealed class ModerationRequestDto
{
    public int Id { get; init; }

    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public ModerationStatus Status { get; init; }

    public Guid RequestedBy { get; init; }

    public Guid? ReviewedBy { get; init; }

    public string? ReviewNote { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CreateModerationRequest
{
    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;
}

public sealed class ReviewModerationRequest
{
    public string? ReviewNote { get; init; }
}
