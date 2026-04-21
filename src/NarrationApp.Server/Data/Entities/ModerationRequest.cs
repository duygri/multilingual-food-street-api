using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class ModerationRequest
{
    public int Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public ModerationStatus Status { get; set; }

    public Guid RequestedBy { get; set; }

    public Guid? ReviewedBy { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser? RequestedByUser { get; set; }

    public AppUser? ReviewedByUser { get; set; }
}
