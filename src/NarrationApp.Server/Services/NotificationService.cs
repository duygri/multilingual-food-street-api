using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class NotificationService(AppDbContext dbContext, INotificationBroadcaster broadcaster) : INotificationService
{
    public async Task<NotificationDto> CreateAsync(Guid userId, NotificationType type, string title, string message, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = notification.ToDto();
        await broadcaster.BroadcastAsync(userId, dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(item => item.ToDto()).ToArray();
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);
        return new UnreadCountDto { Count = count };
    }

    public async Task MarkReadAsync(Guid userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification was not found.");

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Notifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.IsRead = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification was not found.");

        dbContext.Notifications.Remove(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
