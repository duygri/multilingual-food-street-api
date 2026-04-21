using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.Web.Services;

public interface INotificationRealtimeService
{
    event Action<NotificationDto>? NotificationReceived;

    ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default);

    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
}
