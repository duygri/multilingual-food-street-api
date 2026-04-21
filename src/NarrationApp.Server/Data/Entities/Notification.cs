using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class Notification
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser? User { get; set; }
}
