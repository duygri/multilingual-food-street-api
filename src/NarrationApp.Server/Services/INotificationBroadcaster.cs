using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.Server.Services;

public interface INotificationBroadcaster
{
    Task BroadcastAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
