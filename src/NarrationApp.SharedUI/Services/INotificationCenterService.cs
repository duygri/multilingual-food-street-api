using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.SharedUI.Services;

public interface INotificationCenterService
{
    event Action? Changed;

    ValueTask<IReadOnlyList<NotificationDto>> GetAsync(CancellationToken cancellationToken = default);

    ValueTask<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    ValueTask MarkAllReadAsync(CancellationToken cancellationToken = default);

    ValueTask MarkReadAsync(int notificationId, CancellationToken cancellationToken = default);
}
