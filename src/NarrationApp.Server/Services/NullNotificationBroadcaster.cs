using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.Server.Services;

public sealed class NullNotificationBroadcaster : INotificationBroadcaster
{
    public Task BroadcastAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
