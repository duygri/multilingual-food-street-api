using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(Guid userId, NotificationType type, string title, string message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid userId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, int notificationId, CancellationToken cancellationToken = default);
}
