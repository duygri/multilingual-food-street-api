using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Notification;

public sealed class NotificationDto
{
    public int Id { get; init; }

    public Guid UserId { get; init; }

    public NotificationType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class UnreadCountDto
{
    public int Count { get; init; }
}
